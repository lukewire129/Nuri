const vscode = require("vscode");
const childProcess = require("child_process");
const crypto = require("crypto");
const fs = require("fs");
const http = require("http");
const os = require("os");
const path = require("path");

const protocol = "nuri-preview-v1";
const previewRenderers = {
  duxel: {
    id: "duxel",
    displayName: "Duxel",
    executableName: "Nuri.Duxel.PreviewHost.exe",
    installedDirectoryName: "duxel-preview-host",
    sourceProjectParts: ["src", "Nuri.Duxel", "Nuri.Duxel.PreviewHost"],
    targetFrameworks: ["net8.0-windows", "net9.0-windows"],
  },
  wpf: {
    id: "wpf",
    displayName: "WPF",
    executableName: "Nuri.WPF.PreviewHost.exe",
    installedDirectoryName: "preview-host",
    sourceProjectParts: ["src", "Nuri.WPF.PreviewHost"],
    targetFrameworks: ["net8.0-windows", "net9.0-windows"],
  },
};
let previewProcess;
let previewConnection;
let previewPanel;
let previewProjectPath;
let commandFilePath;
let sessionDirectory;
let outputChannel;
let statusBarItem;
let extensionContext;
let stopping = false;
const expectedExitProcesses = new Set();

function activate(context) {
  extensionContext = context;
  outputChannel = vscode.window.createOutputChannel("Nuri Preview");
  statusBarItem = vscode.window.createStatusBarItem(
    vscode.StatusBarAlignment.Right,
    100
  );
  statusBarItem.command = "nuri.preview.focus";

  context.subscriptions.push(
    outputChannel,
    statusBarItem,
    vscode.commands.registerCommand("nuri.preview.start", startPreview),
    vscode.commands.registerCommand("nuri.preview.refresh", () =>
      requestRefresh("full")
    ),
    vscode.commands.registerCommand("nuri.preview.focus", focusNativePreview),
    vscode.commands.registerCommand("nuri.preview.stop", () => stopPreview()),
    vscode.workspace.onDidSaveTextDocument(onDocumentSaved)
  );
}

async function deactivate() {
  await stopPreview();
}

async function startPreview() {
  if (!vscode.workspace.isTrusted) {
    vscode.window.showWarningMessage(
      "Nuri Preview requires a trusted workspace because it compiles and runs project code."
    );
    return;
  }

  const editor = vscode.window.activeTextEditor;
  if (!editor || path.extname(editor.document.uri.fsPath).toLowerCase() !== ".cs") {
    vscode.window.showWarningMessage(
      "Open a C# file in a Nuri project before starting the preview."
    );
    return;
  }

  const projectPath = findProjectForFile(editor.document.uri.fsPath);
  if (!projectPath) {
    vscode.window.showWarningMessage(
      "Nuri Preview could not find a .csproj file for the active C# file."
    );
    return;
  }

  const rendererSelection = selectPreviewRenderer(projectPath);
  if (!rendererSelection.renderer) {
    vscode.window.showErrorMessage(rendererSelection.error);
    return;
  }

  const renderer = rendererSelection.renderer;
  const previewHostPath = resolvePreviewHostPath(contextOrThrow(), renderer, projectPath);
  if (!previewHostPath) {
    vscode.window.showErrorMessage(
      `${renderer.executableName} was not found. Build the preview host or set nuri.preview.previewHostPath.`
    );
    return;
  }

  await stopPreview();
  stopping = false;
  previewProjectPath = projectPath;
  sessionDirectory = fs.mkdtempSync(
    path.join(os.tmpdir(), "nuri-vscode-preview-")
  );
  commandFilePath = path.join(sessionDirectory, "preview.cmd");
  const statusFilePath = path.join(sessionDirectory, "preview.status");
  const connectionFilePath = path.join(sessionDirectory, "preview.connection.json");
  const configuredFramesPerSecond = vscode.workspace
    .getConfiguration("nuri.preview")
    .get("framesPerSecond", 15);
  const framesPerSecond = Math.max(
    1,
    Math.min(30, Number.isInteger(configuredFramesPerSecond) ? configuredFramesPerSecond : 15)
  );

  const args = [
    "--project",
    projectPath,
    "--command-file",
    commandFilePath,
    "--status-file",
    statusFilePath,
    "--connection-file",
    connectionFilePath,
    "--capture-fps",
    String(framesPerSecond),
  ];

  outputChannel.appendLine(
    `[nuri] Starting ${previewHostPath} ${args.map(quoteForLog).join(" ")}`
  );
  const launchedProcess = childProcess.spawn(previewHostPath, args, {
    cwd: path.dirname(projectPath),
    stdio: ["ignore", "pipe", "pipe"],
    windowsHide: false,
  });
  previewProcess = launchedProcess;

  launchedProcess.stdout?.on("data", (data) => outputChannel.append(data.toString()));
  launchedProcess.stderr?.on("data", (data) => outputChannel.append(data.toString()));
  launchedProcess.on("error", (error) => {
    outputChannel.appendLine(`[nuri] PreviewHost failed to start: ${error}`);
    vscode.window.showErrorMessage(`Nuri PreviewHost failed to start: ${error.message}`);
  });
  launchedProcess.on("exit", (code) => {
    const expected = expectedExitProcesses.delete(launchedProcess);
    const wasCurrent = previewProcess === launchedProcess;
    outputChannel.appendLine(`[nuri] PreviewHost exited with code ${code}`);
    if (wasCurrent) {
      previewProcess = undefined;
      previewConnection = undefined;
    }
    if (!expected && wasCurrent) {
      statusBarItem.text = "$(error) Nuri Preview stopped";
      previewPanel?.webview.postMessage({
        type: "hostStopped",
        code,
      });
    }
  });

  statusBarItem.text = "$(loading~spin) Nuri Preview: Starting";
  statusBarItem.show();

  try {
    previewConnection = await waitForConnectionFile(connectionFilePath, 15000);
  } catch (error) {
    outputChannel.appendLine(`[nuri] ${error}`);
    vscode.window.showErrorMessage(String(error));
    await stopPreview();
    return;
  }

  showPreviewPanel(previewConnection);
  statusBarItem.text = `$(eye) Nuri ${renderer.displayName}: ${path.basename(projectPath, ".csproj")}`;
  statusBarItem.tooltip = `Click to focus the native Nuri ${renderer.displayName} preview window.`;
}

function findProjectForFile(filePath) {
  const workspaceFolder = vscode.workspace.getWorkspaceFolder(vscode.Uri.file(filePath));
  const boundary = workspaceFolder?.uri.fsPath;
  let directory = path.dirname(filePath);

  while (true) {
    let projects = [];
    try {
      projects = fs
        .readdirSync(directory)
        .filter((entry) => entry.toLowerCase().endsWith(".csproj"))
        .sort();
    } catch {
      return undefined;
    }

    if (projects.length > 0) {
      const nuriProject = projects.find((project) => {
        try {
          return /(?:PackageReference|ProjectReference)[^>]+Nuri/i.test(
            fs.readFileSync(path.join(directory, project), "utf8")
          );
        } catch {
          return false;
        }
      });
      return path.join(directory, nuriProject ?? projects[0]);
    }

    if (boundary && samePath(directory, boundary)) return undefined;
    const parent = path.dirname(directory);
    if (parent === directory) return undefined;
    directory = parent;
  }
}

function selectPreviewRenderer(projectPath) {
  const referenceNames = collectProjectReferenceNames(projectPath, new Set());
  const hasWpf = referenceNames.has("nuri.wpf");
  const hasDuxel =
    referenceNames.has("nuri.duxel") ||
    referenceNames.has("nuri.duxel.windows");

  if (hasWpf && hasDuxel) {
    const sourceRendererId = detectPreviewRendererFromSource(projectPath);
    if (sourceRendererId) {
      return { renderer: previewRenderers[sourceRendererId] };
    }

    const directReferenceNames = collectProjectReferenceNames(
      projectPath,
      new Set(),
      false
    );
    const directlyReferencesWpf = directReferenceNames.has("nuri.wpf");
    const directlyReferencesDuxel =
      directReferenceNames.has("nuri.duxel") ||
      directReferenceNames.has("nuri.duxel.windows");
    if (directlyReferencesWpf !== directlyReferencesDuxel) {
      return {
        renderer: directlyReferencesWpf ? previewRenderers.wpf : previewRenderers.duxel,
      };
    }

    return {
      error:
        "Multiple Nuri preview renderers were detected: WPF, Duxel. Qualify the startup call with Nuri.WPF.NuriApplication or Nuri.Duxel.NuriApplication, or keep only one direct renderer reference.",
    };
  }

  if (hasDuxel) {
    return { renderer: previewRenderers.duxel };
  }

  return { renderer: previewRenderers.wpf };
}

function collectProjectReferenceNames(projectPath, visitedProjects, includeTransitive = true) {
  const names = new Set();
  const fullProjectPath = path.resolve(projectPath);
  const visitKey = fullProjectPath.toLowerCase();
  if (visitedProjects.has(visitKey) || !fs.existsSync(fullProjectPath)) return names;
  visitedProjects.add(visitKey);

  let projectText;
  try {
    projectText = fs.readFileSync(fullProjectPath, "utf8");
  } catch {
    return names;
  }

  const projectDirectory = path.dirname(fullProjectPath);
  const itemPattern = /<(PackageReference|Reference|ProjectReference)\b[^>]*>/gi;
  for (const match of projectText.matchAll(itemPattern)) {
    const includeMatch = /\b(?:Include|Update)\s*=\s*["']([^"']+)["']/i.exec(match[0]);
    if (!includeMatch) continue;

    const include = includeMatch[1].trim();
    if (match[1].toLowerCase() !== "projectreference") {
      names.add(include.split(",", 1)[0].trim().toLowerCase());
      continue;
    }

    const referencedProjectPath = path.resolve(projectDirectory, include);
    names.add(path.basename(referencedProjectPath, path.extname(referencedProjectPath)).toLowerCase());
    if (includeTransitive) {
      for (const name of collectProjectReferenceNames(referencedProjectPath, visitedProjects, true)) {
        names.add(name);
      }
    }
  }

  return names;
}

function detectPreviewRendererFromSource(projectPath) {
  const detected = new Set();
  for (const sourcePath of collectProjectSourceFiles(path.dirname(path.resolve(projectPath)))) {
    let source;
    try {
      source = fs.readFileSync(sourcePath, "utf8");
    } catch {
      continue;
    }

    if (
      /\b(?:global::)?Nuri\.WPF\.NuriApplication\s*\.\s*(?:Create|Run|Show)\s*(?:<|\()/.test(source) ||
      /\bNuriApplication\s*\.\s*Create\s*(?:<|\()/.test(source)
    ) {
      detected.add("wpf");
    }

    if (/\b(?:global::)?Nuri\.Duxel\.NuriApplication\s*\.\s*Run\s*(?:<|\()/.test(source)) {
      detected.add("duxel");
    }

    if (!/\bNuriApplication\s*\.\s*(?:Run|Show)\s*(?:<|\()/.test(source)) continue;

    const usesWpf = /\busing\s+(?:global::)?Nuri\.WPF\s*;/.test(source);
    const usesDuxel = /\busing\s+(?:global::)?Nuri\.Duxel\s*;/.test(source);
    if (usesWpf && !usesDuxel) detected.add("wpf");
    if (usesDuxel && !usesWpf) detected.add("duxel");
  }

  return detected.size === 1 ? detected.values().next().value : undefined;
}

function collectProjectSourceFiles(projectDirectory) {
  const sourceFiles = [];
  const pending = [projectDirectory];
  const excludedDirectories = new Set(["bin", "obj", ".git", ".vs", "node_modules"]);

  while (pending.length > 0) {
    const directory = pending.pop();
    let entries;
    try {
      entries = fs.readdirSync(directory, { withFileTypes: true });
    } catch {
      continue;
    }

    for (const entry of entries) {
      if (entry.isDirectory()) {
        if (!excludedDirectories.has(entry.name.toLowerCase())) {
          pending.push(path.join(directory, entry.name));
        }
      } else if (entry.isFile() && entry.name.toLowerCase().endsWith(".cs")) {
        sourceFiles.push(path.join(directory, entry.name));
      }
    }
  }

  return sourceFiles;
}

function resolvePreviewHostPath(context, renderer, projectPath) {
  const configuredPath = vscode.workspace
    .getConfiguration("nuri.preview")
    .get("previewHostPath", "")
    .trim();
  const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
  const targetFrameworks = orderTargetFrameworks(projectPath, renderer.targetFrameworks);
  const candidates = [configuredPath];

  for (const targetFramework of targetFrameworks) {
    candidates.push(
      path.join(
        context.extensionPath,
        renderer.installedDirectoryName,
        targetFramework,
        renderer.executableName
      )
    );
  }
  candidates.push(
    path.join(context.extensionPath, renderer.installedDirectoryName, renderer.executableName)
  );

  if (workspaceRoot) {
    for (const configuration of ["Release", "Debug"]) {
      for (const targetFramework of targetFrameworks) {
        candidates.push(
          path.join(
            workspaceRoot,
            ...renderer.sourceProjectParts,
            "bin",
            configuration,
            targetFramework,
            renderer.executableName
          )
        );
      }
    }
  }

  return candidates
    .filter((candidate) => candidate && path.isAbsolute(candidate))
    .find((candidate) => fs.existsSync(candidate));
}

function orderTargetFrameworks(projectPath, candidates) {
  let projectText;
  try {
    projectText = fs.readFileSync(projectPath, "utf8");
  } catch {
    return candidates;
  }

  const singleTarget = /<TargetFramework\b[^>]*>([^<]+)<\/TargetFramework>/i.exec(projectText)?.[1];
  const multipleTargets = /<TargetFrameworks\b[^>]*>([^<]+)<\/TargetFrameworks>/i.exec(projectText)?.[1];
  const targetFramework = (singleTarget || multipleTargets?.split(";", 1)[0] || "").trim();
  const preferred = /^net9\.0(?:-|$)/i.test(targetFramework)
    ? "net9.0-windows"
    : /^net8\.0(?:-|$)/i.test(targetFramework)
      ? "net8.0-windows"
      : undefined;
  if (!preferred || !candidates.includes(preferred)) return candidates;

  return [preferred, ...candidates.filter((candidate) => candidate !== preferred)];
}

async function waitForConnectionFile(connectionFilePath, timeoutMilliseconds) {
  const deadline = Date.now() + timeoutMilliseconds;
  while (Date.now() < deadline) {
    if (previewProcess?.exitCode !== null && previewProcess?.exitCode !== undefined) {
      throw new Error(`Nuri PreviewHost exited with code ${previewProcess.exitCode}.`);
    }

    try {
      const text = fs.readFileSync(connectionFilePath, "utf8").replace(/^\uFEFF/, "");
      const connection = JSON.parse(text);
      if (
        connection.protocol === protocol &&
        Number.isInteger(connection.port) &&
        connection.port > 0 &&
        connection.port <= 65535 &&
        typeof connection.token === "string" &&
        /^[A-Za-z0-9_-]+$/.test(connection.token)
      ) {
        return connection;
      }
    } catch {
      // The host may still be writing the atomic connection file.
    }

    await delay(100);
  }

  throw new Error("Timed out waiting for the Nuri PreviewHost capture server.");
}

function showPreviewPanel(connection) {
  if (previewPanel) {
    previewPanel.webview.html = createPreviewHtml(connection);
    previewPanel.reveal(vscode.ViewColumn.Beside, true);
    return;
  }

  const panel = vscode.window.createWebviewPanel(
    "nuriPreview",
    "Nuri Preview",
    { viewColumn: vscode.ViewColumn.Beside, preserveFocus: true },
    {
      enableScripts: true,
      retainContextWhenHidden: true,
      localResourceRoots: [],
    }
  );
  previewPanel = panel;
  panel.webview.html = createPreviewHtml(connection);
  panel.webview.onDidReceiveMessage((message) => {
    if (message?.type === "refresh") requestRefresh("full");
  });
  panel.onDidDispose(() => {
    if (previewPanel === panel) previewPanel = undefined;
    if (!stopping) void stopPreview({ disposePanel: false });
  });
}

function createPreviewHtml(connection) {
  const nonce = crypto.randomBytes(16).toString("base64");
  const baseUrl = `http://127.0.0.1:${connection.port}`;
  const authorization = `Bearer ${connection.token}`;

  return `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta http-equiv="Content-Security-Policy" content="default-src 'none'; connect-src ${baseUrl}; img-src blob:; style-src 'unsafe-inline'; script-src 'nonce-${nonce}';">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <style>
    * { box-sizing: border-box; }
    body { margin: 0; height: 100vh; overflow: hidden; color: var(--vscode-foreground); background: var(--vscode-editor-background); font-family: var(--vscode-font-family); }
    .toolbar { height: 34px; display: flex; align-items: center; gap: 8px; padding: 5px 10px; border-bottom: 1px solid var(--vscode-panel-border); }
    .toolbar button { border: 0; border-radius: 2px; padding: 3px 9px; cursor: pointer; color: var(--vscode-button-foreground); background: var(--vscode-button-background); }
    .toolbar button:hover { background: var(--vscode-button-hoverBackground); }
    #status { margin-left: auto; max-width: 70%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-size: 11px; opacity: .8; }
    #status.error { color: var(--vscode-errorForeground); opacity: 1; }
    #status.building { color: var(--vscode-charts-orange); opacity: 1; }
    .surface { height: calc(100vh - 34px); display: flex; align-items: center; justify-content: center; overflow: auto; padding: 10px; }
    #preview { display: none; max-width: 100%; max-height: 100%; object-fit: contain; border: 1px solid var(--vscode-panel-border); cursor: pointer; }
    #placeholder { opacity: .65; text-align: center; }
  </style>
</head>
<body>
  <div class="toolbar">
    <button id="focus">Focus Native Window</button>
    <button id="refresh">Refresh</button>
    <span id="status">Connecting...</span>
  </div>
  <div class="surface">
    <img id="preview" alt="Nuri Preview" title="Click to focus the native preview window">
    <div id="placeholder">Waiting for the first preview frame...</div>
  </div>
  <script nonce="${nonce}">
    const vscode = acquireVsCodeApi();
    const baseUrl = ${JSON.stringify(baseUrl)};
    const authorization = ${JSON.stringify(authorization)};
    const headers = { Authorization: authorization };
    const preview = document.getElementById('preview');
    const placeholder = document.getElementById('placeholder');
    const status = document.getElementById('status');
    let currentFrameUrl;
    let frameVersion = 0;
    let stopped = false;

    async function refreshFrame() {
      if (stopped) return;
      if (document.hidden) {
        setTimeout(refreshFrame, 500);
        return;
      }
      try {
        const response = await fetch(
          baseUrl + '/frame?after=' + encodeURIComponent(frameVersion),
          { headers, cache: 'no-store' }
        );
        if (response.status === 200) {
          const receivedVersion = Number(response.headers.get('X-Nuri-Frame-Version'));
          const nextFrameUrl = URL.createObjectURL(await response.blob());
          preview.onload = () => {
            if (currentFrameUrl) URL.revokeObjectURL(currentFrameUrl);
            currentFrameUrl = nextFrameUrl;
          };
          preview.src = nextFrameUrl;
          preview.style.display = 'block';
          placeholder.style.display = 'none';
          if (Number.isSafeInteger(receivedVersion) && receivedVersion > frameVersion) {
            frameVersion = receivedVersion;
          }
        }
      } catch {
        status.textContent = 'Preview disconnected';
        status.className = 'error';
        setTimeout(refreshFrame, 500);
        return;
      }
      setTimeout(refreshFrame, 0);
    }

    async function refreshStatus() {
      if (stopped) return;
      try {
        const response = await fetch(baseUrl + '/status', { headers, cache: 'no-store' });
        if (response.ok) {
          const value = await response.json();
          status.textContent = value.message || 'Preview ready';
          status.className = value.hasError ? 'error' : (value.isBuilding ? 'building' : '');
        }
      } catch { }
      setTimeout(refreshStatus, 500);
    }

    function focusNativeWindow() {
      fetch(baseUrl + '/focus', { method: 'POST', headers }).catch(() => {});
    }

    document.getElementById('focus').addEventListener('click', focusNativeWindow);
    document.getElementById('refresh').addEventListener('click', () => {
      vscode.postMessage({ type: 'refresh' });
    });
    preview.addEventListener('click', focusNativeWindow);
    window.addEventListener('message', (event) => {
      if (event.data?.type === 'hostStopped') {
        stopped = true;
        status.textContent = 'PreviewHost stopped';
        status.className = 'error';
      }
    });
    refreshFrame();
    refreshStatus();
  </script>
</body>
</html>`;
}

function onDocumentSaved(document) {
  const refreshOnSave = vscode.workspace
    .getConfiguration("nuri.preview")
    .get("refreshOnSave", true);
  if (!refreshOnSave || !previewProjectPath || !commandFilePath) return;

  const savedPath = document.uri.fsPath;
  if (path.extname(savedPath).toLowerCase() !== ".cs") return;
  if (!isPathInside(savedPath, path.dirname(previewProjectPath))) return;

  requestRefresh("partial");
}

function requestRefresh(kind) {
  if (!commandFilePath || !previewProcess) {
    vscode.window.showWarningMessage("No Nuri preview is running.");
    return;
  }

  try {
    fs.writeFileSync(commandFilePath, `${kind} ${Date.now()}`, "utf8");
    outputChannel.appendLine(`[nuri] Requested ${kind} preview refresh.`);
  } catch (error) {
    outputChannel.appendLine(`[nuri] Could not request preview refresh: ${error}`);
  }
}

async function focusNativePreview() {
  if (!previewConnection) {
    vscode.window.showWarningMessage("No Nuri preview is running.");
    return;
  }

  try {
    await postToPreview(previewConnection, "/focus");
  } catch {
    vscode.window.showWarningMessage("Could not focus the Nuri preview window.");
  }
}

function postToPreview(connection, requestPath) {
  return new Promise((resolve, reject) => {
    const request = http.request(
      {
        hostname: "127.0.0.1",
        port: connection.port,
        path: requestPath,
        method: "POST",
        headers: { Authorization: `Bearer ${connection.token}` },
        timeout: 3000,
      },
      (response) => {
        response.resume();
        response.on("end", () => {
          if (response.statusCode >= 200 && response.statusCode < 300) resolve();
          else reject(new Error(`HTTP ${response.statusCode}`));
        });
      }
    );
    request.on("error", reject);
    request.on("timeout", () => request.destroy(new Error("timeout")));
    request.end();
  });
}

async function stopPreview(options = {}) {
  const disposePanel = options.disposePanel !== false;
  stopping = true;

  const hostProcess = previewProcess;
  previewProcess = undefined;
  previewConnection = undefined;
  previewProjectPath = undefined;
  commandFilePath = undefined;

  if (disposePanel && previewPanel) {
    const panel = previewPanel;
    previewPanel = undefined;
    panel.dispose();
  }

  if (hostProcess?.pid) {
    expectedExitProcesses.add(hostProcess);
    try {
      if (globalThis.process.platform === "win32") {
        childProcess.spawnSync(
          "taskkill",
          ["/T", "/F", "/PID", String(hostProcess.pid)],
          { stdio: "ignore", windowsHide: true }
        );
      } else {
        hostProcess.kill();
      }
    } catch {
      try {
        hostProcess.kill();
      } catch { }
    }
  }

  cleanupSessionDirectory(sessionDirectory);
  sessionDirectory = undefined;
  statusBarItem?.hide();
  stopping = false;
}

function cleanupSessionDirectory(directory) {
  if (!directory) return;
  const resolved = path.resolve(directory);
  if (
    !samePath(path.dirname(resolved), path.resolve(os.tmpdir())) ||
    !path.basename(resolved).startsWith("nuri-vscode-preview-")
  ) {
    outputChannel?.appendLine(`[nuri] Refusing to remove unexpected session path: ${resolved}`);
    return;
  }

  try {
    fs.rmSync(resolved, { recursive: true, force: true });
  } catch (error) {
    outputChannel?.appendLine(`[nuri] Could not remove session directory: ${error}`);
  }
}

function isPathInside(candidate, parent) {
  const relative = path.relative(path.resolve(parent), path.resolve(candidate));
  return relative === "" || (!relative.startsWith("..") && !path.isAbsolute(relative));
}

function samePath(left, right) {
  return path.resolve(left).toLowerCase() === path.resolve(right).toLowerCase();
}

function quoteForLog(value) {
  return /\s/.test(value) ? JSON.stringify(value) : value;
}

function delay(milliseconds) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}

function contextOrThrow() {
  if (!extensionContext) throw new Error("Nuri Preview extension is not active.");
  return extensionContext;
}

module.exports = { activate, deactivate };
