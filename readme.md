Work in Progress

This is a multithreaded application written in C# 
Given a set of root directories, it recursively identifies duplicate files, which the user can then choose to keep, move, or remove.


Instructions to use this utility:
Pull the repository to your local machine
From the terminal, navigate to the repository folder.
Run: 
```
dotnet run --root=.
dotnet run --root=C:/Photos
```

_Windows Powershell_: To load default Development configuration, first run the following to set the env variable accordingly:
```shell
$env:DOTNET_ENVIRONMENT="Development"
```
_Linux/MacOS_:
```shell
export DOTNET_ENVIRONMENT="Development"
```
