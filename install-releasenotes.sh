#!/bin/sh

command -v rnotes >/dev/null 2>&1 && CUR_VERSION="$(rnotes --version)" || CUR_VERSION="v0.0.0"
VERSION_TO_INSTALL=${1:-latest}
RELEASE_URL="https://api.github.com/repos/fazzani/az-release-notes/releases/tags/$VERSION_TO_INSTALL"
[ $VERSION_TO_INSTALL = "latest" ] && RELEASE_URL="https://api.github.com/repos/fazzani/az-release-notes/releases/latest"
NEW_VERSION="$(curl -s $RELEASE_URL | jq -r '.tag_name')"
echo "Current Version: $CUR_VERSION => New Version: $NEW_VERSION"

if [ "$NEW_VERSION" != "$CUR_VERSION" ]; then

  echo "Installing version $NEW_VERSION"

  cd /tmp/

  curl -s $RELEASE_URL \
  | grep "browser_download_url.*rnotes-linux-x64\.zip" \
  | cut -d ":" -f 2,3 \
  | tr -d \" \
  | xargs wget -qi -O rnotes-linux-x64.zip

  unzip rnotes-linux-x64.zip && rm rnotes-linux-x64.zip

  [ ! -d $HOME/.rnotes ] && mkdir $HOME/.rnotes
  mv ReleaseNotes/* $HOME/.rnotes

  [ -L /usr/local/bin/rnotes ] || sudo ln -s $HOME/.rnotes/rnotes /usr/local/bin/rnotes
  # sudo mv /usr/local/bin/ReleaseNotes /usr/local/bin/rnotes

  chmod +x $HOME/.rnotes/rnotes

  cd -

  location="$(which rnotes)"
  echo "rnotes binary location: $location"

  version="$(rnotes --version)"
  echo "New rnotes binary version installed!: $version"

else
  echo "Latest version already installed"
fi