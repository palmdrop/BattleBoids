public class Command
{
    private bool isCommandPressed = false;

    public void CommandPressed()
    {
        isCommandPressed = true;
    }

    public void CommandComplete()
    {
        isCommandPressed = false;
    }

    
    public bool IsCommandKeyPressed()
    {
        return isCommandPressed;
    }
}