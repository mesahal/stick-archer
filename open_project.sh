#!/bin/bash
# Run this script once Unity 2022.3 is installed to open the project

UNITY_HUB_CLI=~/Applications/Unity\ Hub.app/Contents/MacOS/Unity\ Hub
PROJECT_PATH="/Users/bs01100/projects/my/stick-archer"

echo "Checking for installed Unity editors..."
"$UNITY_HUB_CLI" -- --headless editors -i 2>&1 | grep -v "ERROR\|LevelDB\|leveldb\|LOCK"

echo ""
echo "Opening project in Unity..."
"$UNITY_HUB_CLI" -- --headless open --path "$PROJECT_PATH" 2>&1 | grep -v "ERROR\|LevelDB\|leveldb\|LOCK"
