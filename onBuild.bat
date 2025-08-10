mkdir sim
cd sim
"..\..\AIPSim\AIPilot.exe" "..\simConfig.json" > sim.log
"..\..\HeadlessClient\HeadlessClient.exe" --convert --input recording.json --output result.vtgr --map "../../Map/"
cd ..