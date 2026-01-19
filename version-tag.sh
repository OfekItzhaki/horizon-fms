#!/bin/bash
# Automatic version tagging script
# Usage: ./version-tag.sh [patch|minor|major] [message]

VERSION_TYPE=${1:-patch}
MESSAGE=${2:-"Auto version bump"}

# Get current version tag
CURRENT_TAG=$(git describe --tags --abbrev=0 2>/dev/null || echo "v0.0.0")

# Extract version numbers
CURRENT_VERSION=${CURRENT_TAG#v}
IFS='.' read -ra VERSION_PARTS <<< "$CURRENT_VERSION"
MAJOR=${VERSION_PARTS[0]:-0}
MINOR=${VERSION_PARTS[1]:-0}
PATCH=${VERSION_PARTS[2]:-0}

# Bump version based on type
case $VERSION_TYPE in
  major)
    MAJOR=$((MAJOR + 1))
    MINOR=0
    PATCH=0
    ;;
  minor)
    MINOR=$((MINOR + 1))
    PATCH=0
    ;;
  patch)
    PATCH=$((PATCH + 1))
    ;;
  *)
    echo "Invalid version type. Use: patch, minor, or major"
    exit 1
    ;;
esac

NEW_VERSION="v${MAJOR}.${MINOR}.${PATCH}"

# Create tag
git tag -a "$NEW_VERSION" -m "$MESSAGE"

echo "Created tag: $NEW_VERSION"
echo "To push: git push origin $NEW_VERSION"
