using System.IO;
using System.Xml.Serialization;
using UnityModManagerNet;

namespace DontSayAnythingGuys
{
    public class Settings : UnityModManager.ModSettings
    {
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            var filepath = Path.Combine(modEntry.Path, "Settings.xml");
            using (var writer = new StreamWriter(filepath))
                new XmlSerializer(GetType()).Serialize(writer, this);
        }

        public string token;
        public bool showToken = false;

        public string channelId;
        public string userId;

        public int tile = 1;
        public bool unmuteOnEnd = true;

        public bool runOnLaunch = false;
    }
}
