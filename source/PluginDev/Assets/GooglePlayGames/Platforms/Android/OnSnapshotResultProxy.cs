
#if UNITY_ANDROID
using UnityEngine;
using GooglePlayGames.BasicApi;
using GooglePlayGames.OurUtils;

namespace GooglePlayGames.Android {
    internal class OnSnapshotResultProxy : AndroidJavaProxy {
        protected AndroidClient mClient;
        protected OnSnapshotResultListener mListener;

        internal OnSnapshotResultProxy(AndroidClient client, OnSnapshotResultListener listener) :
            base(JavaConsts.ResultCallbackClass) {
            mClient = client;
            mListener = listener;
        }
    }

    internal class OnOpenResultProxy : OnSnapshotResultProxy {
        internal OnOpenResultProxy(AndroidClient client, OnSnapshotResultListener listener) :
            base(client, listener) {}

        private void resolveConflict(string conflictId, SnapshotAndroid r)
        {
            mClient.CallClientApi("open snapshot metadata", () => {
                mClient.GHManager.CallGmsApiWithResult("games.Games", "Snapshots", "resolveConflict", 
                    new OnOpenResultProxy(mClient, mListener), conflictId, r.javaObj());
            }, null);
        }

        public void onResult(AndroidJavaObject result)
        {
            Logger.d("OnOpenResultProxy.onResult, result=" + result);

            if (result == null) {
                Logger.e("OnOpenResultProxy: result is null.");
                return;
            }

            int statusCode = JavaUtil.GetStatusCode(result);
            Logger.d("OnOpenResultProxy: status code is " + statusCode);

            AndroidJavaObject openedResult =
                JavaUtil.CallNullSafeObjectMethod(result, "getSnapshot");
            AndroidJavaObject conflictResult =
                JavaUtil.CallNullSafeObjectMethod(result, "getConflictingSnapshot");

            Snapshot opened = null;
            Snapshot conflict = null;

            if(conflictResult != null) {
                conflict = new SnapshotAndroid(mClient, conflictResult);
                opened = new SnapshotAndroid(mClient, openedResult);
                string conflictId = result.Call<string>("getConflictId");
                if (mListener != null) {
                    Logger.d("OnOpenResultProxy.onResult invoke conflict callback.");
                    PlayGamesHelperObject.RunOnGameThread(() => {
                        SnapshotAndroid r = mListener.OnSnapshotConflict(conflict, opened) as SnapshotAndroid;
                        resolveConflict(conflictId, r);
                    });
                }
            } else if(openedResult != null) {
                opened = new SnapshotAndroid(mClient, openedResult);
                if (mListener != null) {
                    Logger.d("OnOpenResultProxy.onResult invoke opened callback.");
                    PlayGamesHelperObject.RunOnGameThread(() => {
                        mListener.OnSnapshotOpened(statusCode, opened);
                    });
                }
            } else {
                Logger.d("OnOpenResultProxy: both openedResult and conflictResult are null!");
                if (mListener != null) {
                    Logger.d("OnOpenResultProxy.onResult invoke fail callback.");
                    PlayGamesHelperObject.RunOnGameThread(() => {
                        mListener.OnSnapshotOpened(statusCode, null);
                    });
                }
            }
        
        }
    }

    internal class OnCommitResultProxy : OnSnapshotResultProxy {
        internal OnCommitResultProxy(AndroidClient client, OnSnapshotResultListener listener) :
            base(client, listener) {}

        public void onResult(AndroidJavaObject result) {
            Logger.d("OnCommitResultProxy.onResult, result=" + result);
            
            if (result == null) {
                Logger.e("OnCommitResultProxy: result is null.");
                return;
            }
            
            int statusCode = JavaUtil.GetStatusCode(result);
            Logger.d("OnCommitResultProxy: status code is " + statusCode);

            if(mListener != null) {
                Logger.d("OnCommitResultProxy.onResult invoke callback.");
                PlayGamesHelperObject.RunOnGameThread(() => {
                    mListener.OnSnapshotCommitted(statusCode);
                });
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
            if(mListener != null) {
                Logger.d("SelectSnapshotProxy.OnSelectSnapshotResult invoke callback.");
                SnapshotMetadataAndroid meta = success ? new SnapshotMetadataAndroid(mClient, metadata) :  null;
                mListener.OnSelectSnapshotResult(success, meta, isNew);
            } 
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
                    "games.Games", "Snapshots", "open", 
                    new OnOpenResultProxy(mClient, listener), mMetaObj
                );
            }, null);
        }

        public override void open(OnSnapshotResultListener listener,
                                  string fileName, bool createIfNotFound) {
            Logger.d("SnapshotMetadataAndroid.open");
            
            mClient.CallClientApi("open snapshot metadata", () => {
                mClient.GHManager.CallGmsApiWithResult(
                    "games.Games", "Snapshots", "open", 
                    new OnOpenResultProxy(mClient, listener), fileName, createIfNotFound
                    );
            }, null);
        }

        public override SnapshotMetadataChange.Builder change() {
            return new SnapshotMetadataChangeAndroid.Builder();
        }

        public override string getDescription() {
            return mMetaObj.Call<string>("getDescription");
        }

        public override long getDuration() {
            return mMetaObj.Call<long>("getDuration");
        }

        public override long getLastModifiedTimestamp() {
            return mMetaObj.Call<long>("getLastModifiedTimestamp");
        }
    }

    internal class SnapshotMetadataChangeAndroid : SnapshotMetadataChange {
        internal new class Builder : SnapshotMetadataChange.Builder {
            private AndroidJavaObject mObj;

            public Builder() {
                mObj = new AndroidJavaObject(JavaConsts.SnapshotMetadataChangeBuilderClass);
            }

            public override SnapshotMetadataChange.Builder setCoverImage(string path) {
                AndroidJavaObject bitmap = JavaUtil.GetBitmapFromPath(path);
                if(bitmap != null)
                    mObj = mObj.Call<AndroidJavaObject>("setCoverImage", bitmap);                 
                return this;
            }
            public override SnapshotMetadataChange.Builder setDescription(string description) {
                mObj = mObj.Call<AndroidJavaObject>("setDescription", description);
                return this;
            }
            public override SnapshotMetadataChange.Builder setDurationMillis(long durationMillis) {
                mObj = mObj.Call<AndroidJavaObject>("setPlayedTimeMillis", durationMillis);
                return this;
            }

            public override SnapshotMetadataChange build() {
                return new SnapshotMetadataChangeAndroid(mObj.Call<AndroidJavaObject>("build"));
            }
        }

        private AndroidJavaObject mChangeObj;

        public AndroidJavaObject javaObj() { return mChangeObj; }

        internal SnapshotMetadataChangeAndroid(AndroidJavaObject obj) {
            mChangeObj = obj;
        }

        public override SnapshotMetadataChange.Builder newBuilder() {
            return new Builder();
        }
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

            mClient.CallClientApi("Commit snapshot", () => {
                mClient.GHManager.CallGmsApiWithResult(
                    "games.Games", "Snapshots", "commitAndClose", 
                    new OnCommitResultProxy(mClient, listener),
                    mObj, change.javaObj()
                    );
            }, null);
        }

        public override void discardAndClose() {
            mClient.CallClientApi("discard snapshot", () => {
                mClient.GHManager.CallGmsApi(
                    "games.Games", "Snapshots", "discardAndClose", mObj
                );
            }, null);
        }

        public override byte[] readFully()
        {
            AndroidJavaObject byteArrayObj = mObj.Call<AndroidJavaObject>("readFully");

            return JavaUtil.ConvertByteArray(byteArrayObj);
        }

        public override bool writeBytes(byte[] content)
        {
            return mObj.Call<bool>("writeBytes", content);
        }
    }
}
#endif
