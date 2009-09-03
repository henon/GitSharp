namespace GitSharp.CLI
{
	public class OptionContext
	{
		public OptionContext(OptionSet set)
		{
			OptionSet = set;
			OptionValues = new OptionValueCollection(this);
		}

		public Option Option { get; set; }
		public string OptionName { get; set; }
		public int OptionIndex { get; set; }
		public OptionSet OptionSet { get; private set; }
		public OptionValueCollection OptionValues { get; private set; }
	}
}