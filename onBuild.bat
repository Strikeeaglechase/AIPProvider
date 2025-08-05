mkdir sim
cd sim
"..\..\AIPSim\AIPilot.exe" --allied "../bin/Debug/net6.0/AIPProvider.dll" --enemy "../bin/Debug/net6.0/AIPProvider.dll" --debug-enemy --map "../../Map/" > sim.log
"..\..\HeadlessClient\HeadlessClient.exe" --convert --input recording.json --output result.vtgr --map "../../Map/"
cd ..