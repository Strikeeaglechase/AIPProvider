using Recorder;
using System.Runtime.Loader;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPLoader
{
    class ActionList : List<int>
    {
        public void Add(InboundAction action)
        {
            Add((int)action);
        }
    }

    enum WeaponType
    {
        Heat,
        Radar
    }


    public class AIPProvider : IAIPProvider
    {
        private OutboundState state;
        private InboundState output;
        private ActionList actions = new ActionList();

        private int selectedWeaponIdx = 0;
        private PID pitchPid = new PID(0.3f, 0.05f, 0f);

        private Team team;

        private float test = 0;
        private float testRate = 0;

        private Dictionary<WeaponType, string> weaponClassifications = new Dictionary<WeaponType, string>
        {
            { WeaponType.Radar, "Weapons/Missiles/AIM-120" },
            { WeaponType.Heat, "Weapons/Missiles/AIRS-T" }
        };

        public override SetupActions Start(SetupInfo info)
        {
            team = info.team;
            testRate = TestingValue("testRate", 1, -1, 1, 0.01f);
            return new SetupActions
            {
                hardpoints = new string[] { "HPEquips/AFighter/af_amraamRail", "HPEquips/AFighter/fa26_iris-t-x1" },
                name = "AI Client"
            };
        }

        private void SelectWeapon(int weaponIdx)
        {
            actions.Add(InboundAction.SelectHardpoint);
            actions.Add(weaponIdx);
            Log($"Selecting weapon idx {weaponIdx}");
            selectedWeaponIdx = weaponIdx;
        }

        private void SelectWeapon(string weapon)
        {
            var weaponIdx = Array.IndexOf(state.weapons, weapon);
            if (weaponIdx == -1 || weaponIdx == selectedWeaponIdx) return;

            SelectWeapon(weaponIdx);
        }

        private bool HasWeaponOfType(WeaponType type)
        {
            return state.weapons.Contains(weaponClassifications[type]);
        }

        private void SelectWeaponOfType(WeaponType type)
        {
            SelectWeapon(weaponClassifications[type]);
        }

        private void AutoTWS()
        {
            foreach (var dt in state.radar.detectedTargets)
            {
                var isTwsed = state.radar.twsedTargets.Any(std => std.id == dt.id);
                if (!isTwsed)
                {
                    actions.Add(InboundAction.RadarTWS);
                    actions.Add(dt.id);
                }
            }
        }

        private void AutoSTT()
        {
            if (state.radar.sttedTarget != null) return;
            if (state.radar.detectedTargets.Length == 0) return;

            actions.Add(InboundAction.RadarSTT);
            actions.Add(state.radar.detectedTargets[0].id);
        }

        private void SlaveIRToVisual()
        {
            bool hasVt = false;
            VisuallySpottedTarget vt = default;
            foreach (var visTarget in state.visualTargets)
            {
                if (visTarget.team != team && visTarget.type == VisualTargetType.Aircraft)
                {
                    hasVt = true;
                    vt = visTarget;
                }
            }

            if (hasVt)
            {
                output.irLookDir = vt.direction;
            }
        }

        private void Fly()
        {
            var altTarget = 7000;
            var targetAngles = new Vector3(state.kinematics.position.y < altTarget ? -25 : 0, state.kinematics.rotation.quat.eulerAngles.y, 0);
            var targetForward = Quaternion.Euler(targetAngles) * Vector3.forward;
            var currentForward = state.kinematics.rotation.quat * Vector3.forward;
            var rotation = Quaternion.FromToRotation(currentForward, targetForward);
            rotation.ToAngleAxis(out float errorAngle, out Vector3 axis);


            var pidResult = pitchPid.Update(errorAngle, Time.fixedDeltaTime);
            output.pyr.x = axis.x * pidResult;

            Graph("pyr", output.pyr);
        }

        private void RunWeaponEngagement()
        {
            if (state.radar.sttedTarget == null) return;

            var target = state.radar.sttedTarget.Value;
            var distToTarget = (target.position.vec3 - state.kinematics.position.vec3).magnitude;

            if (distToTarget < 9260 && HasWeaponOfType(WeaponType.Heat))
            {
                // 5nm
                SlaveIRToVisual();
                if (state.ir.heat > 500)
                {
                    SelectWeaponOfType(WeaponType.Heat);
                    actions.Add(InboundAction.Fire);
                }
            }
            else if (distToTarget < 37040 && HasWeaponOfType(WeaponType.Radar))
            {
                // 20nm
                SelectWeaponOfType(WeaponType.Radar);
                actions.Add(InboundAction.Fire);
            }
        }

        public override InboundState Update(OutboundState state)
        {
            this.state = state;
            output = new InboundState
            {
                pyr = new NetVector { x = 0, y = 0, z = 0 },
                throttle = 100,
            };

            AutoSTT();
            Fly();
            RunWeaponEngagement();
            test += testRate;
            Graph("test", test);
            //var line = new DebugLine(42)
            //{
            //    start = state.kinematics.position,
            //    end = new NetVector(state.kinematics.position.vec3 + state.kinematics.velocity.vec3 * 1000),
            //};

            //DebugShape(line);

            output.events = actions.ToArray();
            actions.Clear();

            return output;
        }
    }
}
