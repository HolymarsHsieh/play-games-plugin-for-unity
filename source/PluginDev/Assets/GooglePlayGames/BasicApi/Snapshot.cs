
namespace GooglePlayGames.BasicApi {
    public abstract class Snapshot {
        public abstract void commitAndClose(OnSnapshotResultListener listener,
                                            SnapshotMetadataChange metadataChange);
        public abstract void discardAndClose();
        public abstract byte[] readFully();
        public abstract bool writeBytes(byte[] content);
    }

    public abstract class SnapshotMetadata {
        public abstract void open(OnSnapshotResultListener listener);
        public abstract SnapshotMetadataChange.Builder change();
        public abstract string getDescription();
        public abstract long getDuration();
        public abstract long getLastModifiedTimestamp();
    }

    public abstract class SnapshotMetadataChange {
        public abstract class Builder {
            public abstract SnapshotMetadataChange build();
            public abstract Builder setCoverImage(string path);
            public abstract Builder setDescription(string description);
            public abstract Builder SettingsDurationMillis(long durationMillis);
        }
    }
}
