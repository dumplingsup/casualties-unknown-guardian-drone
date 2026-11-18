using System.Runtime.CompilerServices;
using CUCoreLib.Saving;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GuardianDrone
{
    public sealed class DroneState
    {
        public bool Deployed;
        public bool Guard = true;
        public bool HadController;
        public bool Unsupported;
        public int Version = 1;
        public JObject Original = new JObject();
    }

    public sealed class DroneSave : IBodySaveProvider
    {
        public const string Key = "local.cu.guardiandrone.state";
        public static float LastRestoreAt = float.NegativeInfinity;
        public static int RestoreSerial;
        static readonly ConditionalWeakTable<Body, DroneState> States = new ConditionalWeakTable<Body, DroneState>();
        int capturedVersion = 1;
        public static DroneState For(Body body) => States.GetOrCreateValue(body);
        public static bool RestoredWithin(float now, float seconds) => now - LastRestoreAt < seconds;
        public int GetVersion() => capturedVersion;
        public JToken Capture(Body body)
        {
            var state = For(body);
            capturedVersion = state.Version;
            var result = (JObject)state.Original.DeepClone();
            if (!state.Unsupported)
            {
                result["schemaVersion"] = 1;
                result["deployed"] = state.Deployed;
                result["guard"] = state.Guard;
                result["hadController"] = state.HadController;
            }
            return result;
        }
        public void Restore(Body body, JToken payload, int version, SaveRestoreContext context)
        {
            var state = For(body);
            LastRestoreAt = Time.unscaledTime;
            RestoreSerial++;
            state.Original = payload is JObject obj ? (JObject)obj.DeepClone() : new JObject();
            state.Version = version;
            var schema = state.Original["schemaVersion"];
            state.Unsupported = version != 1 || (schema != null && (schema.Type != JTokenType.Integer || schema.Value<long>() != 1));
            state.Deployed = !state.Unsupported && state.Original["deployed"]?.Type == JTokenType.Boolean && (bool)state.Original["deployed"];
            state.Guard = state.Original["guard"]?.Type != JTokenType.Boolean || (bool)state.Original["guard"];
            // 0.2.0 saves have no hadController field. A deployed drone proves the
            // player owned the controller when that save was written, so treat it
            // as owned and let the live scan find the item again after loading.
            var had = state.Original["hadController"];
            state.HadController = !state.Unsupported &&
                (had?.Type == JTokenType.Boolean ? (bool)had : state.Deployed);
            Plugin.Log("RESTORE deployed=" + state.Deployed + " guard=" + state.Guard + " hadController=" + state.HadController + " unsupported=" + state.Unsupported);
        }
    }
}
