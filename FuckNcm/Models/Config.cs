namespace FuckNcm.Models;

public class Config
{
    public bool DarkTheme { get; set; } = true;

    public string LastSourceDir { get; set; } = string.Empty;

    public string LastTargetDir { get; set; } = string.Empty;

    public string BackgroundImagePath { get; set; } = string.Empty;
}