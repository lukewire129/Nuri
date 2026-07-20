const childProcess = require("child_process");
const fs = require("fs");
const path = require("path");

const extensionDirectory = path.resolve(__dirname, "..");
const repositoryDirectory = path.resolve(extensionDirectory, "..", "..");
const hosts = [
  {
    project: path.join(repositoryDirectory, "src", "Nuri.WPF.PreviewHost", "Nuri.WPF.PreviewHost.csproj"),
    outputRoot: path.join(repositoryDirectory, "src", "Nuri.WPF.PreviewHost", "bin", "Release"),
    targetFrameworks: ["net8.0-windows", "net9.0-windows"],
    destination: path.join(extensionDirectory, "preview-host"),
  },
  {
    project: path.join(
      repositoryDirectory,
      "src",
      "Nuri.Duxel",
      "Nuri.Duxel.PreviewHost",
      "Nuri.Duxel.PreviewHost.csproj"
    ),
    outputRoot: path.join(
      repositoryDirectory,
      "src",
      "Nuri.Duxel",
      "Nuri.Duxel.PreviewHost",
      "bin",
      "Release"
    ),
    targetFrameworks: ["net8.0-windows", "net9.0-windows"],
    destination: path.join(extensionDirectory, "duxel-preview-host"),
  },
];

for (const host of hosts) {
  const result = childProcess.spawnSync(
    "dotnet",
    [
      "build",
      host.project,
      "-c",
      "Release",
      "--disable-build-servers",
      "-m:1",
      "-nr:false",
      "-p:UseSharedCompilation=false",
    ],
    { cwd: repositoryDirectory, stdio: "inherit", windowsHide: true }
  );
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);

  fs.rmSync(host.destination, { recursive: true, force: true });
  for (const targetFramework of host.targetFrameworks) {
    fs.cpSync(
      path.join(host.outputRoot, targetFramework),
      path.join(host.destination, targetFramework),
      {
        recursive: true,
        filter: (source) =>
          !source.toLowerCase().endsWith(".pdb") &&
          !source.toLowerCase().endsWith(".resources.dll"),
      }
    );
  }
}
