mkdir sim
cd sim
"..\..\AIPSim\AIPilot.exe" --allied "../bin/Debug/net6.0/AIPLoader.dll" --enemy "../bin/Debug/net6.0/AIPLoader.dll" --debug-enemy --map "../../Map/"
"..\..\HeadlessClient\HeadlessClient.exe" --convert --input recording.json --output result.vtgr --map "../../Map/"
cd ..