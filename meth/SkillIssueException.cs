namespace BlockGame.util;

/**
 * For when you have a skill issue. This isn't the fault of the user or the game, it's your fault as a developer.
 * <param name="message">The description of the skill issue.</param>
 * */
public class SkillIssueException(string message) : Exception(message) {
    
    /**
     * These throwers exist because throwing exceptions fucks codegen.
     */
    public static void throwNew() {
        throw new SkillIssueException("You have a skill issue.");
    }
    
    
    /**
     * These throwers exist because throwing exceptions fucks codegen.
     */
    public static void throwNew(string message) {
        throw new SkillIssueException(message);
    }
}

/**
 * For when the user has input something invalid.
 * Remember, we respect our users, they don't have skill issues, they make mistakes like the rest of us.
 * As a developer, you should catch these and handle them gracefully, with your sweet warmth and embrace.
 */
public class InputException : ArgumentException {
    
    
    public InputException(string message) : base(message) {
    }
    
    public InputException(string message, Exception ex) : base(message, ex) {
    }

    public static void throwNew() {
        throw new InputException("Some of the input is not valid.");
    }
    
    public static void throwNew(string message) {
        throw new InputException(message);
    }
}