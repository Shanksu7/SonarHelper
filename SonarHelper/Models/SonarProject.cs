using System;

namespace SonarHelper.Models
{
    public class SonarProject
    {
        public string ProjectName { get; set; }
        public string SonarToken { get; set; }
        public string ProjectPath { get; set; }
        public DateTime? LastAnalysisDate { get; set; }

        public SonarProject()
        {
            ProjectName = string.Empty;
            SonarToken = string.Empty;
            ProjectPath = string.Empty;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (SonarProject)obj;
            return ProjectName == other.ProjectName ||
                   SonarToken == other.SonarToken ||
                   ProjectPath == other.ProjectPath;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ProjectName, SonarToken, ProjectPath);
        }
    }
} 