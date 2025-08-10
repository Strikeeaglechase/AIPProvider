# AIPProvider

This project is an example AI Pilot demonstrating the basics.

## The IAIPProvider

The only requirement for an AIP is to be a C# DLL that contains a class implementing the IAIPProvider abstract class (note this is an actual class not an interface).

You should not call or reference anything outside of the methods provided by IAIPProvider, with the exception of types as required.

The minimal possible AIP is as follows:
(Note the usings, "UnityGERunner" generally refers to the AIPSim namespace, for technical reasons it retains this name)

```csharp
using UnityGERunner;
using UnityGERunner.UnityApplication;

public class AIPProvider : IAIPProvider {
	public override SetupActions Start(SetupInfo info)
	{
		return new SetupActions { name = "AI Client" };
	}

	public override InboundState Update(OutboundState state)
	{
		return new InboundState {
			pyr = new NetVector { x = 0, y = 0, z = 0 },
			throttle = 100
		}
	}
}

```

`Start` is called when the bot is spawned, it receives a `SetupInfo` struct which contains useful info, such as what team you are on and fight configuration, and allows you to name the AI and chose what weapons to spawn with.

`Update` is called on every frame, it's the beating heart of your AIP. It receives a struct containing all sensor/data the aircraft knows about, and returns any action the AI should be taking.

Additionally a `Stop` method exists and is called before the program exits if you need to do any cleanup.

All distances/speeds are in meters or meters per seconds. All angles are in degrees.

## Setup

```csharp
enum Team {
	Allied = 0,
	Enemy = 1
	Unknown = 2
}

struct SetupInfo
{
	Team team; // Team the AI is spawned on
	int id; // AI client Id
	float spawnDist; // Spawn radius in meters
	NetVector mapCenterPoint; // Center point of the map
	string mapPath; // Path to the map heightmap
	string mapId;
	int alliedSpawns; // Number of allied aircraft
	int enemySpawns; // Number of enemy aircraft
	Dictionary<string, int> weaponRestrictions; // Map of WeaponPath to maximum number of that weapon you can have. If a weapon is not in this map it is unrestricted.
	string[] args; // Command line arguments passed to the simulation
}

struct SetupActions
{
	string[] hardpoints; // List of hardpoints you would like, which spawns weapons onto the aircraft
	string name; // AI Client name, used for replay identification
	float fuel; // Set's the spawn fuel level in liters, 7100 max
}
```

## Update

"Inbound" and "Outbound" are named from the perspective of the AIPSim, "outbound" means "going out of the sim"

```csharp
struct OutboundState
{
	Kinematics kinematics; // Aircraft physical state
	RadarState radar; // Radar data
	StateRWRContact[] rwrContacts; // Contacts on the RWR
	VisuallySpottedTarget[] visualTargets; // Proximity visually spotted targets
	IRWeaponState ir; // State of the selected heat seeker's sensor
	DatalinkState datalink; // Any shared datalink information (also contains info you have contributed)
	KillFeedEntry[] killFeed; // Kill feed entries

	string[] weapons; // List of non-fired weapons on the aircraft
	int selectedWeapon; // Index of the currently selected weapon (not guaranteed to be bound correctly)

	int flareCount;
	int chaffCount;
	int gunAmmo; // Remaining gun ammunition

	float fuel; // Fuel remaining in liters
	float time; // Sim time in seconds
}

struct Kinematics
{
	NetVector position;
	NetQuaternion rotation;
	NetVector velocity;
}

struct RadarState
{
	float angle;
	float elevationAdjust; // The set elevation offset from the horizon
	float azimuthAdjust; // The set azimuth offset from directly forward
	float fov; // Current set FOV
	int pdt; // PDT index, -1 if no PDT is set

	StateTargetData[] twsedTargets; // Full data of all TWS selected targets
	MinimalDetectedTargetData[] detectedTargets; // Partial data of targets found in a scan, can use radar angle to resolve bearing but TWS is required to get actual data
	StateTargetData? sttedTarget; // Hardlock target data if it exists
}

struct StateTargetData
{
	NetVector position;
	NetVector velocity;
	NetQuaternion rotation;
	Team team;
	int id;
}

struct MinimalDetectedTargetData
{
	int id;
	Team team;
	float detectedTime;
}

struct StateRWRContact
{
	float detectedTime; // Time in seconds when the ping occurred
	float signalStrength; // Signal strength (with some amount of precision loss)
	int actorId;
	bool isLock; // If it is a hard lock
	float bearing; // Bearing (with some amount of precision loss)
	Team team;
	bool isMissile;
}

struct VisuallySpottedTarget
{
	VisualTargetType type; // Enum, 0=Aircraft,1=Missile
	NetVector direction;
	NetQuaternion orientation; // Orientation/rotation of the spotted target
	float closure; // Closure rate
	int id;
	Team team;
}

// IR Weapon state is only valid if the currently selected weapon is an IR missile
struct IRWeaponState
{
	float seekerFov; // Seeker FOV, which is a constant however allows the state replay tool to not have to make assumptions
	float heat; // Heat received by the sensor
	NetVector lookDir; // Current seeker look direction
}

struct RadarDLData { StateTargetData data; int contributedBy; }
struct VisualDLData { VisuallySpottedTarget data; int contributedBy; }
struct FriendlyData { int id; NetVector position; NetVector velocity; }

struct DatalinkState
{
   RadarDLData[] radar; // Only contains TWS or STT data. It is possible for multiple aircraft to see a target in different places due to ECM
   VisualDLData[] visual; // Visual data only provides a direction to a target, that direction is relative to the contributor's position so you must correct for that via the friendly data
   FriendlyData[] friendlies; // Contains all friendly aircraft
}

struct KillFeedEntry
{
	int entityId; // ID of the entity that was destroyed
	float time; // Time when the kill occurred
}

```

```csharp
struct InboundState
	{
   public NetVector pyr; // Stick position (pitch, yaw, roll) (-1, 1)
   public float throttle; // Throttle setting, 0-1, above 0.75 is afterburner

   public NetVector irLookDir; // Command look direction for an IR seeker

   public int[] events; // List of actions you wish to make, see below list
}
```

### Action Table

The `events` array is constructed via this table:

| ID  | Name            | Argument | Description                                                       |
| --- | --------------- | -------- | ----------------------------------------------------------------- |
| 0   | RadarState      | Power    | Sets the radar on or off (0 or 1)                                 |
| 1   | RadarSTT        | Id       | Starts STTing a target                                            |
| 2   | RadarStopSTT    | N/A      | Drops current STT                                                 |
| 3   | RadarTWS        | Id       | Starts TWSing a target                                            |
| 4   | RadarDropTWS    | Id       | Stops TWS for a target                                            |
| 5   | RadarSetPDT     | Idx      | Sets a TWS target as PDT based on index, -1 to select STT         |
| 6   | RadarElevation  | Angle    | Sets radar elevation offset                                       |
| 7   | RadarAzimuth    | Angle    | Sets radar azimuth offset                                         |
| 8   | RadarFov        | Angle    | Sets radar FOV                                                    |
| 9   | Fire            | N/A      | Launches currently selected missile                               |
| 10  | Flare           | N/A      | Deploys a flare                                                   |
| 11  | Chaff           | N/A      | Deploys a chaff countermeasure                                    |
| 12  | ChaffFlare      | N/A      | Deploys one of both chaff and flares                              |
| 13  | SelectHardpoint | Idx      | Chooses active weapon (-1 for gun)                                |
| 14  | SetUncage       | Uncage   | Set's IR seeker to uncaged (follows heat independently) (or or 1) |

### Terrain

Two utility functions exist to help sample the terrain, additionally you may load the heightmap directly from the setup method.

```csharp
float HeightAt(NetVector pt); // Returns the hight above ground level (AGL)
bool Linecast(NetVector a, NetVector b, out NetVector hitPoint); // Checks if a line from point A to point B intersects the terrain, returns the hit point if it does.
// Note: Because I am stupid this function has variable time execution based on the length of the line you are sampling.
```

### NetVector, NetColor, NetQuaternion

The "Net" classes exist due to serialization constraints, especially in regards to online-AIP, they are lightweight structs that do not provide any functionality beyond storing the basic component values. Each struct has an accessor to retrieve a unity-like struct with the proper methods and functionality you would expect:

```csharp
Vector3 v3 = new NetVector().vec3;
Quaternion q = new NetQuaternion().quat;
Color c = new Color().col;
```

Each type has implicit operators for conversion as well so it may be possible to just outright pass them around without needing to convert them.

## Debugging

AIP provides several debugging utilities. Most of these functions do not work if the AIP does not have debugging enabled (ie via `--debug-allied`). The AIPProvider can also set debugging on/off by modifying the local property `outputEnabled`. It is highly recommended only to use this to conditionally set to false, and use CLI flags to set it to true, otherwise sharing your AI may result in you overwriting other user's debug information.

### Graphing

There are three graph methods, when called they automatically save the value to a file that the HC Graph View can visualize

```csharp
void Graph(string key, Vector3 value);
void Graph(string key, NetVector3 value);
void Graph(string key, float value);
```

### Logging

`Log` can be called to save a timestamped message to a file

```csharp
void Log(string message);
```

### Debug shapes

A debug shape is either a DebugLine or DebugSphere, which is geometry that will be visible within the HC replay viewer scene. These shapes are referenced via an ID that can be used to update the same shape without having to totally recreate it. All properties of a debug shape except for it's ID is optional. When updating debug shapes the properties you specify will be updated, anything unspecified will remain what was previously set.

> [!NOTE]
> The ID of these shapes shares the same ID space as entities within the replay, currently it is possible to set a debug shape to overlap IDs with an existing entity. The solution for now is to set a relatively high ID as entity ids start at 0 and are incremental.

```csharp
void DebugShape(DebugLine line);
void DebugShape(DebugSphere sphere);
void RemoveDebugShape(int shapeId);

class DebugLine : RecorderEvent
{
	NetVector? start;
	NetVector? end;
	NetColor? color;
	int id;
}

class DebugSphere : RecorderEvent
{
	NetVector? pos;
	int? size;
	NetColor? color;
	int id;
}
```

## Rapid Value Testing (RVT)

RVT is a system that allows quickly fine tuning/testing values in your AIP without having to recompile/manually rerun every time. It's primarily intended for testing things like PIDs, however you can use it to test any value.

In this mode, variables you define will be exposed in the HC graph view as a slider, and when you change the value it will automatically rerun the AIP with the new value.

To define a testing value you call the `TestingValue` method in your AIPProvider startup method, giving it a name and default value. The return of this method is the current slider set value (which will default to the default value you set).

```csharp
float TestingValue(string name, float defaultValue, float minValue, float maxValue, float step = 0.1f)
```

You enable RVT via the `--rvt` command line flag with HC, and as an argument give a AIPSim start command. Ie:

```bash
HeadlessClient.exe --rvt "../AIPilot/AIPilot.exe --enemy ../../AIPProvider/bin/Debug\net6.0/AIPProvider.dll --debug-enemy --no-map"
```
