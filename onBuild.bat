mkdir sim
cd sim
"C:\Users\strik\Desktop\Programs\CSharp\UnityGERunner\bin\Debug\net6.0\UnityGERunner.exe" --allied ../bin/Debug/net6.0/AIPLoader.dll --enemy ../bin/Debug/net6.0/AIPLoader.dll --debug-enemy --no-auto-vtgr
"C:\Users\strik\Desktop\Programs\Typescript\VTOLLiveViewer\VTOLLiveViewerClient\out\Headless Client-win32-x64\HeadlessClient.exe" --convert --input recording.json --output result.vtgr --map "C:\Users\strik\Desktop\Programs\CSharp\UnityGERunner\src\Application\Resources\Map"