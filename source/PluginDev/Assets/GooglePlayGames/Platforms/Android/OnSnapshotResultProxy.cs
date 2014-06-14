
#if UNITY_ANDROID
using UnityEngine;
using GooglePlayGames.BasicApi;
using GooglePlayGames.OurUtils;

namespace GooglePlayGames.Android {
    internal class OnSnapshotResultProxy : AndroidJavaProxy {
        private AndroidClient mClient;
        private OnSnapshotResultListener mListener;

        internal OnSnapshotResultProxy(AndroidClient client, OnSnapshotResultListener listener) :
            base(JavaConsts.ResultCallbackClass) {
            mClient = client;
            mListener = listener;
        }

        private void resolveConflict(string conflictId, SnapshotAndroid r)
        {
            mClient.CallClientApi("open snapshot metadata", () => {
                mClient.GHManager.CallGmsApiWithResult("games.Games", "Snapshots", "resolveConflict", 
                 new OnSnapshotResultProxy(mClient, mListener), conflictId, r.javaObj());
            }, null);
        }

        public void onResult(AndroidJavaObject result)
        {
            Logger.d("OnStateResultProxy.onResult, result=" + result);
            
            int statusCode = JavaUtil.GetStatusCode(result);
            Logger.d("OnStateResultProxy: status code is " + statusCode);

            if (result == null) {
                Logger.e("OnStateResultProxy: result is null.");
                return;
            }

            AndroidJavaObject openedResult =
                JavaUtil.CallNullSafeObjectMethod(result, "getSnapshot");
            AndroidJavaObject conflictResult =
                JavaUtil.CallNullSafeObjectMethod(result, "getConflictingSnapshot");

            Snapshot opened = null;
            Snapshot conflict = null;
            if(conflictResult != null) {
                conflict = new SnapshotAndroid(mClient, conflictResult);
                string conflictId = result.Call<string>("getConflictId");
                if (mListener != null) {
                    Logger.d("OnSnapshotResultProxy.onResult invoke conflict callback.");
                    PlayGamesHelperObject.RunOnGameThread(() => {
                        SnapshotAndroid r = mListener.OnSnapshotConflict(conflict, opened) as SnapshotAndroid;
                        resolveConflict(conflictId, r);
                    });
                }
            } else if(openedResult != null) {
                opened = new SnapshotAndroid(mClient, openedResult);
                if (mListener != null) {
                    Logger.d("OnSnapshotResultProxy.onResult invoke opened callback.");
                    PlayGamesHelperObject.RunOnGameThread(() => {
                        mListener.OnSnapshotOpened(statusCode, opened);
                    });
                }
            } else {
                Logger.d("OnSnapshotResultProxy: both openedResult and conflictResult are null!");
                if (mListener != null) {
                    Logger.d("OnSnapshotResultProxy.onResult invoke fail callback.");
                    PlayGamesHelperObject.RunOnGameThread(() => {
                        mListener.OnSnapshotOpened(statusCode, null);
                    });
                }
            }
        
        }
    }

    internal class SelectSnapshotProxy : AndroidJavaProxy {
        private AndroidClient mClient;
        private OnSnapshotResultListener mListener;
        internal SelectSnapshotProxy(AndroidClient client, OnSnapshotResultListener listener) :
            base(JavaConsts.SupportSelectSnapshotHelperActivityListener) {
            mClient = client;
            mListener = listener;
        }

        public void OnSelectSnapshotResult(bool success, AndroidJavaObject metadata, bool isNew)
        {
            SnapshotMetadataAndroid meta = new SnapshotMetadataAndroid(mClient, metadata);
            mListener.OnSelectSnapshotResult(success, meta, isNew);
        }
    }

    internal class SnapshotMetadataAndroid : SnapshotMetadata {
        private AndroidClient mClient;
        private AndroidJavaObject mMetaObj;

        internal SnapshotMetadataAndroid(AndroidClient client, AndroidJavaObject metaObj) {
            mClient = client;
            mMetaObj = metaObj;
        }

        public override void open(OnSnapshotResultListener listener) {
            Logger.d("SnapshotMetadataAndroid.open");

            mClient.CallClientApi("open snapshot metadata", () => {
                mClient.GHManager.CallGmsApiWithResult(
                    "games.Games", "Snapshots", "load", 
                    new OnSnapshotResultProxy(mClient, listener), mMetaObj
                );
            }, null);
        }
    }

    internal class SnapshotMetadataChangeAndroid : SnapshotMetadataChange {
        private AndroidClient mClient;
        private AndroidJavaObject mChangeObj;

        public AndroidJavaObject javaObj() { return mChangeObj; }
    }

    internal class SnapshotAndroid : Snapshot {
        private AndroidClient mClient;
        private AndroidJavaObject mObj;

        internal SnapshotAndroid(AndroidClient client, AndroidJavaObject obj) {
            mClient = client;
            mObj = obj;
        }

        public AndroidJavaObject javaObj() { return mObj; }

        public override void commitAndClose(OnSnapshotResultListener listener,
                                            SnapshotMetadataChange metadataChange) {
            var change = metadataChange as SnapshotMetadataChangeAndroid;

            mClient.CallClientApi("open snapshot metadata", () => {
                mClient.GHManager.CallGmsApiWithResult(
                    "games.Games", "Snapshots", "commitAndClose", 
                    new OnSnapshotResultProxy(mClient, listener),
                    mObj, change.javaObj()
                    );
            }, null);
        }

        public override void discardAndClose() {
            mClient.CallClientApi("open snapshot metadata", () => {
                mClient.GHManager.CallGmsApi(
                    "games.Games", "Snapshots", "discardAndClose", mObj
                );
            }, null);
        }
    }
}
#endif
