namespace KeelMatrix.QueryWatch
{
    /// <summary>
    /// Provides a simple greeting API to demonstrate the template structure.
    /// </summary>
    public class Hello
    {
        /// <summary>
        /// Returns a friendly greeting message.
        /// </summary>
        /// <param name="name">The name to greet.</param>
        /// <returns>A greeting string.</returns>
        public string Greet(string name)
        {
            return $"Hello, {name}! This is KeelMatrix.QueryWatch.";
        }
    }
}