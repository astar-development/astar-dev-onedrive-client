namespace AStar.Dev.OneDrive.Client.Common;

/// <summary>
///     Defines constant values used throughout the OneDrive client application.
/// </summary>
public static class OneDriveClientConstants
{
    /// <summary>
    ///     Progress reporting constants for controlling UI update frequency.
    /// </summary>
    public static class ProgressReporting
    {
        /// <summary>
        ///     Default number of completed files between progress reports.
        /// </summary>
        public const int DefaultFileInterval = 5;

        /// <summary>
        ///     Default time interval in milliseconds between progress reports.
        /// </summary>
        public const int DefaultMillisecondInterval = 500;
    }

    /// <summary>
    ///     Pagination and batch processing constants.
    /// </summary>
    public static class BatchProcessing
    {
        /// <summary>
        ///     Default page size for downloading files from the database.
        /// </summary>
        public const int DefaultPageSize = 100;
    }
}
