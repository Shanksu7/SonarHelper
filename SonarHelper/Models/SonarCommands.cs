using System.Collections.Generic;

namespace SonarHelper.Models
{
    public class SonarCommand
    {
        public string Name { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public bool Required { get; set; } = true;
    }

    public class SonarCommandsConfig
    {
        public List<SonarCommand> Commands { get; set; } = new List<SonarCommand>();
    }
} 