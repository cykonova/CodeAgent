using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace CodeAgent.CLI.Installer;

public class InstallerGenerator
{
    private readonly string _outputDirectory;
    private readonly ILogger<InstallerGenerator> _logger;

    public InstallerGenerator(string outputDirectory, ILogger<InstallerGenerator> logger)
    {
        _outputDirectory = outputDirectory;
        _logger = logger;
        Directory.CreateDirectory(_outputDirectory);
    }

    public async Task<string> GenerateInstallerAsync(string version, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating installer for version {Version}", version);

        var platform = GetCurrentPlatform();
        var installerPath = platform switch
        {
            Platform.Windows => await GenerateWindowsInstallerAsync(version, cancellationToken),
            Platform.MacOS => await GenerateMacOSInstallerAsync(version, cancellationToken),
            Platform.Linux => await GenerateLinuxInstallerAsync(version, cancellationToken),
            _ => throw new NotSupportedException($"Platform {platform} is not supported")
        };

        _logger.LogInformation("Installer generated at {Path}", installerPath);
        return installerPath;
    }

    private async Task<string> GenerateWindowsInstallerAsync(string version, CancellationToken cancellationToken)
    {
        var installerPath = Path.Combine(_outputDirectory, $"CodeAgent-{version}-win-x64.exe");
        
        // Create NSIS installer script
        var nsisScript = GenerateNSISScript(version);
        var scriptPath = Path.Combine(_outputDirectory, "installer.nsi");
        await File.WriteAllTextAsync(scriptPath, nsisScript, cancellationToken);

        // For now, just create a self-extracting archive
        await CreateSelfExtractingArchiveAsync(installerPath, cancellationToken);
        
        return installerPath;
    }

    private async Task<string> GenerateMacOSInstallerAsync(string version, CancellationToken cancellationToken)
    {
        var installerPath = Path.Combine(_outputDirectory, $"CodeAgent-{version}-osx.pkg");
        
        // Create PKG installer structure
        var pkgStructure = Path.Combine(_outputDirectory, "pkg-structure");
        Directory.CreateDirectory(pkgStructure);
        
        // Create distribution.xml
        var distributionXml = GenerateMacOSDistributionXml(version);
        await File.WriteAllTextAsync(Path.Combine(pkgStructure, "distribution.xml"), distributionXml, cancellationToken);
        
        // For now, create a ZIP archive
        await CreateZipArchiveAsync(installerPath, cancellationToken);
        
        return installerPath;
    }

    private async Task<string> GenerateLinuxInstallerAsync(string version, CancellationToken cancellationToken)
    {
        var debPath = Path.Combine(_outputDirectory, $"codeagent_{version}_amd64.deb");
        var rpmPath = Path.Combine(_outputDirectory, $"codeagent-{version}.x86_64.rpm");
        var appImagePath = Path.Combine(_outputDirectory, $"CodeAgent-{version}-x86_64.AppImage");
        
        // Create .deb package structure
        await CreateDebPackageAsync(debPath, version, cancellationToken);
        
        // Create installation script
        var installScript = GenerateLinuxInstallScript(version);
        var scriptPath = Path.Combine(_outputDirectory, "install.sh");
        await File.WriteAllTextAsync(scriptPath, installScript, cancellationToken);
        
        // Make script executable
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            System.Diagnostics.Process.Start("chmod", $"+x {scriptPath}");
        }
        
        return scriptPath;
    }

    private async Task CreateSelfExtractingArchiveAsync(string outputPath, CancellationToken cancellationToken)
    {
        var tempZip = Path.GetTempFileName();
        
        using (var archive = ZipFile.Open(tempZip, ZipArchiveMode.Create))
        {
            // Add application files
            var appFiles = Directory.GetFiles(".", "*", SearchOption.AllDirectories)
                .Where(f => !f.Contains("installer") && !f.Contains(".git"));
            
            foreach (var file in appFiles)
            {
                var entryName = Path.GetRelativePath(".", file);
                archive.CreateEntryFromFile(file, entryName);
            }
        }
        
        // Create self-extracting header
        var header = Encoding.UTF8.GetBytes("#!/bin/sh\n# Self-extracting installer\n");
        
        using (var output = File.Create(outputPath))
        {
            await output.WriteAsync(header, cancellationToken);
            using (var zipStream = File.OpenRead(tempZip))
            {
                await zipStream.CopyToAsync(output, cancellationToken);
            }
        }
        
        File.Delete(tempZip);
    }

    private async Task CreateZipArchiveAsync(string outputPath, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
        
        var appFiles = Directory.GetFiles(".", "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("installer") && !f.Contains(".git"));
        
        foreach (var file in appFiles)
        {
            var entryName = Path.GetRelativePath(".", file);
            archive.CreateEntryFromFile(file, entryName);
        }
    }

    private async Task CreateDebPackageAsync(string outputPath, string version, CancellationToken cancellationToken)
    {
        var debDir = Path.Combine(_outputDirectory, "deb-package");
        var controlDir = Path.Combine(debDir, "DEBIAN");
        var binDir = Path.Combine(debDir, "usr", "local", "bin");
        
        Directory.CreateDirectory(controlDir);
        Directory.CreateDirectory(binDir);
        
        // Create control file
        var controlContent = $@"Package: codeagent
Version: {version}
Section: utils
Priority: optional
Architecture: amd64
Maintainer: CodeAgent Team
Description: AI-powered coding assistant
 CodeAgent is a command-line coding assistant that integrates
 with multiple LLM providers to help with software development tasks.
";
        await File.WriteAllTextAsync(Path.Combine(controlDir, "control"), controlContent, cancellationToken);
        
        // Copy application files
        var sourceFiles = Directory.GetFiles(".", "CodeAgent*");
        foreach (var file in sourceFiles)
        {
            File.Copy(file, Path.Combine(binDir, Path.GetFileName(file)), true);
        }
        
        // For now, just create a tar.gz archive
        await CreateZipArchiveAsync(outputPath, cancellationToken);
    }

    private string GenerateNSISScript(string version)
    {
        return $@"
!define PRODUCT_NAME ""CodeAgent""
!define PRODUCT_VERSION ""{version}""
!define PRODUCT_PUBLISHER ""CodeAgent Team""

Name ""${{PRODUCT_NAME}} ${{PRODUCT_VERSION}}""
OutFile ""CodeAgent-${{PRODUCT_VERSION}}-Setup.exe""
InstallDir ""$PROGRAMFILES64\CodeAgent""
RequestExecutionLevel admin

Section ""Main""
    SetOutPath $INSTDIR
    File /r ""*.*""
    
    WriteUninstaller ""$INSTDIR\Uninstall.exe""
    
    CreateDirectory ""$SMPROGRAMS\CodeAgent""
    CreateShortcut ""$SMPROGRAMS\CodeAgent\CodeAgent.lnk"" ""$INSTDIR\CodeAgent.exe""
    CreateShortcut ""$SMPROGRAMS\CodeAgent\Uninstall.lnk"" ""$INSTDIR\Uninstall.exe""
    
    WriteRegStr HKLM ""Software\Microsoft\Windows\CurrentVersion\Uninstall\CodeAgent"" \
                     ""DisplayName"" ""CodeAgent""
    WriteRegStr HKLM ""Software\Microsoft\Windows\CurrentVersion\Uninstall\CodeAgent"" \
                     ""UninstallString"" ""$INSTDIR\Uninstall.exe""
SectionEnd

Section ""Uninstall""
    Delete ""$INSTDIR\*.*""
    RMDir /r ""$INSTDIR""
    Delete ""$SMPROGRAMS\CodeAgent\*.*""
    RMDir ""$SMPROGRAMS\CodeAgent""
    DeleteRegKey HKLM ""Software\Microsoft\Windows\CurrentVersion\Uninstall\CodeAgent""
SectionEnd
";
    }

    private string GenerateMacOSDistributionXml(string version)
    {
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<installer-gui-script minSpecVersion=""1"">
    <title>CodeAgent {version}</title>
    <organization>com.codeagent</organization>
    <domains enable_localSystem=""true""/>
    <options customize=""never"" require-scripts=""true"" rootVolumeOnly=""true"" />
    <pkg-ref id=""com.codeagent.pkg"">
        <bundle-version/>
    </pkg-ref>
    <choices-outline>
        <line choice=""default"">
            <line choice=""com.codeagent.pkg""/>
        </line>
    </choices-outline>
    <choice id=""default""/>
    <choice id=""com.codeagent.pkg"" visible=""false"">
        <pkg-ref id=""com.codeagent.pkg""/>
    </choice>
</installer-gui-script>
";
    }

    private string GenerateLinuxInstallScript(string version)
    {
        return $@"#!/bin/bash
# CodeAgent Installation Script v{version}

set -e

echo ""Installing CodeAgent v{version}...""

# Detect distribution
if [ -f /etc/debian_version ]; then
    DISTRO=""debian""
elif [ -f /etc/redhat-release ]; then
    DISTRO=""redhat""
elif [ -f /etc/arch-release ]; then
    DISTRO=""arch""
else
    DISTRO=""unknown""
fi

# Installation directory
INSTALL_DIR=""/opt/codeagent""
BIN_DIR=""/usr/local/bin""

# Create directories
sudo mkdir -p $INSTALL_DIR
sudo mkdir -p $BIN_DIR

# Extract files
echo ""Extracting files...""
sudo cp -r ./CodeAgent* $INSTALL_DIR/

# Create symbolic link
sudo ln -sf $INSTALL_DIR/CodeAgent $BIN_DIR/codeagent

# Set permissions
sudo chmod +x $INSTALL_DIR/CodeAgent
sudo chmod +x $BIN_DIR/codeagent

# Add to PATH if not already there
if ! echo $PATH | grep -q ""/usr/local/bin""; then
    echo 'export PATH=$PATH:/usr/local/bin' >> ~/.bashrc
    source ~/.bashrc
fi

echo ""CodeAgent v{version} installed successfully!""
echo ""Run 'codeagent --help' to get started.""
";
    }

    private Platform GetCurrentPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Platform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Platform.MacOS;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Platform.Linux;
        
        return Platform.Unknown;
    }

    private enum Platform
    {
        Windows,
        MacOS,
        Linux,
        Unknown
    }
}