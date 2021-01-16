#!/bin/bash

command -v rnotes >/dev/null 2>&1 && CUR_VERSION="$(rnotes --version | cut -d'v' -f2 | cut -c 3-5)" || CUR_VERSION="v0.0.0"
NEW_VERSION="$(curl -s https://api.github.com/repos/fazzani/az-release-notes/releases/latest | jq -r '.tag_name')"
echo "Current Version: $CUR_VERSION => New Version: $NEW_VERSION"

if [[ "$NEW_VERSION" != "$CUR_VERSION" ]]; then

  PWD=$(pwd)
  echo "Installing version $NEW_VERSION"

  cd /tmp/

  curl -s https://api.github.com/repos/fazzani/az-release-notes/releases/latest \
  | grep "browser_download_url.*rnotes-linux-x64\.zip" \
  | cut -d ":" -f 2,3 \
  | tr -d \" \
  | xargs wget -qi -O rnotes-linux-x64.zip

  unzip rnotes-linux-x64.zip && rm rnotes-linux-x64.zip

  [[ ! -d $HOME/.rnotes ]] && mkdir $HOME/.rnotes
  mv ReleaseNotes/* $HOME/.rnotes

  [[ -L /usr/local/bin/rnotes ]] || sudo ln -s $HOME/.rnotes/rnotes /usr/local/bin/rnotes
  # sudo mv /usr/local/bin/ReleaseNotes /usr/local/bin/rnotes

  chmod +x $HOME/.rnotes/rnotes

  cd $PWD

  location="$(which rnotes)"
  echo "rnotes binary location: $location"

  version="$(rnotes --version)"
  echo "New rnotes binary version installed!: $version"

else
  echo Latest version already installed
fi