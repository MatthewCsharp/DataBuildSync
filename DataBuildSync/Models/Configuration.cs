namespace DataBuildSync.Models {
    public class Configuration {
        public bool ParallelTransfer { get; set; }
        public string DefaultProjectFolder { get; set; }
        public string DefaultDestinationFolder { get; set; }
        public string LoggingLevel { get; set; }
    }
}