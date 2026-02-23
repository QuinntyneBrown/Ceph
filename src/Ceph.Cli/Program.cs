using System.CommandLine;
using Ceph.Cli.Commands;

var rootCommand = new RootCommand("Ceph CLI – scaffold and manage a Ceph cluster running in Docker on Windows");

rootCommand.AddCommand(new InitCommand());
rootCommand.AddCommand(new DiagnoseCommand());
rootCommand.AddCommand(new FixCommand());

return await rootCommand.InvokeAsync(args);
