namespace SideStream
{
    public class ChatMessage
    {
        /// <summary>
        /// The proportion of words in the sentence with negative valence.
        /// </summary>
        public double NegativeScore { get; set; }

        /// <summary>
        /// The proportion of words in the sentence with no valence.
        /// </summary>
        public double NeutralScore { get; set; }

        /// <summary>
        /// The proportion of words in the sentence with positive valence.
        /// </summary>
        public double PositiveScore { get; set; }

        /// <summary>
        /// Normalized sentiment score between -1 and 1.
        /// </summary>
        public double CompoundScore { get; set; }

        /// <summary>
        /// The received message.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The sender of the message.
        /// </summary>
        public User Sender { get; set; }
    }
}