namespace BlockGame.util;

/// <summary>
/// For when you have a skill issue. This isn't the fault of the user or the game, it's your fault as a developer.
/// </summary>
/// <param name="message">The description of the skill issue.</param>
public class SkillIssueException(string message) : Exception(message) {
    
}
