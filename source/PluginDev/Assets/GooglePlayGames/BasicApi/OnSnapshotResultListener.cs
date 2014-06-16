using System;

namespace GooglePlayGames.BasicApi {
    /// <summary>
    /// Set of callbacks for snapshot
    /// </summary>
    public interface OnSnapshotResultListener {
        void OnSelectSnapshotResult(bool success, SnapshotMetadata meta, bool isNew);
        Snapshot OnSnapshotConflict(Snapshot localData, Snapshot serverData);
        void OnSnapshotOpened(int statusCode, Snapshot snapshot);
        void OnSnapshotCommitted(int statusCode);
    }
}
