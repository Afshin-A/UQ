## Introduction
__Attention!__ This project is a work in progress. Contributions and questions are welcome. 
This is a multithreaded application written in C# 
Given a set of root directories, it recursively identifies duplicate files, which the user can then choose to keep, move, or remove.

## Usage
Instructions to use this utility:
Pull the repository to your local machine
From the terminal, navigate to the repository folder.
Usage:
```shell
dotnet run [directories] [options...]
```
Examples:
```shell
dotnet run
dotnet run c:/ d:/ -w 10
```

## Architecture

## Contribution Notes
### Selecting the right environment
The default hosting environment, defined in `hostingContext.HostingEnvironment.EnvironmentName` is __Production__. Furthermore, The app will load configurations from the `appsettings.Production.json` file. To load __Development__ configuration, you must first set the run the hosting environment following to set the env variable accordingly:
_Windows Powershell_: 
```shell
$env:DOTNET_ENVIRONMENT="Development"
```
_Linux/MacOS_:
```shell
export DOTNET_ENVIRONMENT="Development"
``` 

### Viewing development notes (VS Code)
Additional development notes can be found in the `.out-of-code-insights` directory at the root of the repository. If you're using VS code, you may view these notes throughout the project by installing the _Out-of-Code Insights_ extension. 