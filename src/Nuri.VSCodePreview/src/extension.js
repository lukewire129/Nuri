const vscode = require("vscode");
const childProcess = require("child_process");
const crypto = require("crypto");
const fs = require("fs");
const http = require("http");
const os = require("os");
const path = require("path");

const protocol = "nuri-preview-v1";
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

  const previewHostPath = resolvePreviewHostPath(contextOrThrow());
  if (!previewHostPath) {
    vscode.window.showErrorMessage(
      "Nuri.WPF.PreviewHost.exe was not found. Build the preview host or set nuri.preview.previewHostPath."
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
  statusBarItem.text = `$(eye) Nuri: ${path.basename(projectPath, ".csproj")}`;
  statusBarItem.tooltip = "Click to focus the native Nuri preview window.";
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

function resolvePreviewHostPath(context) {
  const configuredPath = vscode.workspace
    .getConfiguration("nuri.preview")
    .get("previewHostPath", "")
    .trim();
  const workspaceRoot = vscode.workspace.workspaceFolders?.[0]?.uri.fsPath;
  const candidates = [
    configuredPath,
    path.join(context.extensionPath, "preview-host", "Nuri.WPF.PreviewHost.exe"),
  ];

  if (workspaceRoot) {
    candidates.push(
      path.join(
        workspaceRoot,
        "src",
        "Nuri.WPF.PreviewHost",
        "bin",
        "Release",
        "net8.0-windows",
        "Nuri.WPF.PreviewHost.exe"
      ),
      path.join(
        workspaceRoot,
        "src",
        "Nuri.WPF.PreviewHost",
        "bin",
        "Debug",
        "net8.0-windows",
        "Nuri.WPF.PreviewHost.exe"
      )
    );
  }

  return candidates
    .filter((candidate) => candidate && path.isAbsolute(candidate))
    .find((candidate) => fs.existsSync(candidate));
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
