using Ceph.Cli.Services;

namespace Ceph.Cli.Tests;

public class DockerComposeGeneratorTests
{
    [Fact]
    public void Generate_DefaultOptions_CreatesExpectedFiles()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir);

            var files = generator.Generate(options);

            Assert.Equal(6, files.Count);
            Assert.Contains(files, f => f.EndsWith("docker-compose.yml"));
            Assert.Contains(files, f => f.EndsWith("ceph.conf"));
            Assert.Contains(files, f => f.EndsWith("entrypoint.sh"));
            Assert.Contains(files, f => f.EndsWith(".env"));
            Assert.Contains(files, f => f.EndsWith("README.md"));
            Assert.Contains(files, f => f.EndsWith("wslconfig.recommended"));

            foreach (var file in files)
                Assert.True(File.Exists(file), $"Expected file to exist: {file}");
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_DockerCompose_ContainsExpectedServices()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(
                OutputDirectory: outputDir,
                MonitorCount: 2,
                OsdCount: 3,
                MgrCount: 1);

            generator.Generate(options);

            string compose = File.ReadAllText(Path.Combine(outputDir, "docker-compose.yml"));

            Assert.Contains("ceph-mon1", compose);
            Assert.Contains("ceph-mon2", compose);
            Assert.Contains("ceph-mgr1", compose);
            Assert.Contains("ceph-osd1", compose);
            Assert.Contains("ceph-osd2", compose);
            Assert.Contains("ceph-osd3", compose);
            Assert.DoesNotContain("ceph-rgw", compose);
            Assert.DoesNotContain("ceph-mds", compose);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_WithRgwAndMds_IncludesOptionalServices()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(
                OutputDirectory: outputDir,
                IncludeRgw: true,
                IncludeMds: true);

            generator.Generate(options);

            string compose = File.ReadAllText(Path.Combine(outputDir, "docker-compose.yml"));
            Assert.Contains("ceph-rgw", compose);
            Assert.Contains("ceph-mds", compose);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_CephConf_ContainsFsidAndMonHost()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir);

            generator.Generate(options);

            string conf = File.ReadAllText(Path.Combine(outputDir, "ceph.conf"));
            Assert.Contains("fsid", conf);
            Assert.Contains("mon_host", conf);
            Assert.Contains("[global]", conf);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_DockerCompose_UsesSpecifiedImage()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            const string customImage = "quay.io/ceph/ceph:v17";
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(
                OutputDirectory: outputDir,
                CephImage: customImage);

            generator.Generate(options);

            string compose = File.ReadAllText(Path.Combine(outputDir, "docker-compose.yml"));
            Assert.Contains(customImage, compose);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_DockerCompose_HasPrivilegedForOsds()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir, OsdCount: 1);

            generator.Generate(options);

            string compose = File.ReadAllText(Path.Combine(outputDir, "docker-compose.yml"));
            Assert.Contains("privileged: true", compose);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_WslConfig_HasMemorySetting()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir);

            generator.Generate(options);

            string wsl = File.ReadAllText(Path.Combine(outputDir, "wslconfig.recommended"));
            Assert.Contains("[wsl2]", wsl);
            Assert.Contains("memory=", wsl);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_ContainerFiles_HaveUnixLineEndings()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir);

            generator.Generate(options);

            // Files mounted into Linux containers must have LF-only line endings
            string[] containerFiles = ["docker-compose.yml", "ceph.conf", "entrypoint.sh", ".env"];
            foreach (var fileName in containerFiles)
            {
                byte[] bytes = File.ReadAllBytes(Path.Combine(outputDir, fileName));
                bool hasCrlf = false;
                for (int i = 0; i < bytes.Length - 1; i++)
                {
                    if (bytes[i] == 0x0D && bytes[i + 1] == 0x0A)
                    {
                        hasCrlf = true;
                        break;
                    }
                }
                Assert.False(hasCrlf, $"{fileName} contains CRLF line endings – will break in Linux containers");
            }
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_DockerCompose_HasSharedEtcCephVolume()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir);

            generator.Generate(options);

            string compose = File.ReadAllText(Path.Combine(outputDir, "docker-compose.yml"));
            Assert.Contains("ceph-etc:/etc/ceph", compose);
            Assert.Contains("ceph-etc:", compose);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_DockerCompose_HasEntrypointDirective()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir);

            generator.Generate(options);

            string compose = File.ReadAllText(Path.Combine(outputDir, "docker-compose.yml"));
            Assert.Contains("entrypoint: /entrypoint.sh", compose);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_DockerCompose_HasCephPublicNetwork()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir);

            generator.Generate(options);

            string compose = File.ReadAllText(Path.Combine(outputDir, "docker-compose.yml"));
            Assert.Contains("CEPH_PUBLIC_NETWORK=", compose);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_DockerCompose_DependsOnServiceHealthy()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir);

            generator.Generate(options);

            string compose = File.ReadAllText(Path.Combine(outputDir, "docker-compose.yml"));
            Assert.Contains("condition: service_healthy", compose);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }

    [Fact]
    public void Generate_Entrypoint_CopiesSeedConfig()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"ceph-test-{Guid.NewGuid():N}");
        try
        {
            var generator = new DockerComposeGenerator();
            var options = new DockerComposeGenerator.GenerateOptions(OutputDirectory: outputDir);

            generator.Generate(options);

            string entrypoint = File.ReadAllText(Path.Combine(outputDir, "entrypoint.sh"));
            Assert.Contains("ceph.conf.seed", entrypoint);
            Assert.Contains("cp /ceph.conf.seed /etc/ceph/ceph.conf", entrypoint);
        }
        finally
        {
            if (Directory.Exists(outputDir))
                Directory.Delete(outputDir, recursive: true);
        }
    }
}
