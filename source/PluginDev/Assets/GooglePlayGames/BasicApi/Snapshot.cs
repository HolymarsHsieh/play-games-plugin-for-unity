
namespace GooglePlayGames.BasicApi {
    public abstract class Snapshot {
        public abstract void commitAndClose(OnSnapshotResultListener listener,
                                            SnapshotMetadataChange metadataChange);
        public abstract void discardAndClose();
    }

    public abstract class SnapshotMetadata {
        public abstract void open(OnSnapshotResultListener listener);
    }

    public abstract class SnapshotMetadataChange {
    }
}
