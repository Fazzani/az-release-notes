# .Net Azure devops Release notes

[![Build Status](https://dev.azure.com/henifazzani/SynkerAPI/_apis/build/status/ReleaseNotes?repoName=Fazzani%2Faz-release-notes&branchName=master)](https://dev.azure.com/henifazzani/SynkerAPI/_build/latest?definitionId=32&repoName=Fazzani%2Faz-release-notes&branchName=master)

.net core tool that help to generate a release notes from azure devops board.
A new Wiki page will be generated as result.

![overview](https://i.imgur.com/da15IPJ.png)

## Setup

```shell
source <(wget --no-cache -qO- https://gist.github.com/Fazzani/0c8a6d8344aef3256a1836a96dc5f314/raw/)
```

## Getting started

rnotes --help

![help](https://i.imgur.com/WWoDkIK.png)

## Examples

```shell
export SYSTEM_ACCESSTOKEN="AZ-DEVOPS-PAT"
rnotes --organization "https://dev.azure.com/up-group" \
--porject Up.France.ODI \
-r Uptimise \
--team "App - Financeur" \
-n "Home/Applications/Uptimise/ReleaseNotes/" \
-q "4c8cfb27-5726-4124-88ef-a24bd8a3035a" \
-i 0
```