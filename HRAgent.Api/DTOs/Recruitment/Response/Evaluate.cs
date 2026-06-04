namespace HRAgent.Api.DTOs.Recruitment.Response
{
    public class Evaluate
    {
        public int MatchPercentage { get; set; }
        public string Strengths { get; set; } = string.Empty;

        public string MissingSkills { get; set; } = string.Empty;

        public decimal YearsExperience { get; set; } = 0;

        public string Summary { get; set; } = string.Empty;
    }
}
