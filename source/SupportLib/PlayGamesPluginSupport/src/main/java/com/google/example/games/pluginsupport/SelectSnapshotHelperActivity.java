package com.google.example.games.pluginsupport;

import android.app.Activity;
import android.content.Intent;

import com.google.android.gms.games.Games;
import com.google.android.gms.games.snapshot.Snapshots;

public class SelectSnapshotHelperActivity extends UiHelperActivity {
    public static final String EXTRA_TITLE = "EXTRA_TITLE";
    public static final String EXTRA_ALLOW_ADD = "EXTRA_ALLOW_ADD";
    public static final String EXTRA_ALLOW_DELETE = "EXTRA_ALLOW_DELETE";
    public static final String EXTRA_MAX_SNAPSHOTS = "EXTRA_MAX_SNAPSHOTS";

    // Unity's AndroidJavaProxy crashes when you pass null to an Object argument of a listener,
    // so instead of passing nulls, we have to pass this "null object"
    Object mDummyObject = new Object();

    public interface Listener {
        public void OnSelectSnapshotResult(boolean success, Object snapshot, boolean isNew);
    }

    static Listener sListener = null;

    public static void setListener(Listener listener) {
        sListener = listener;
    }

    @Override
    protected void deliverFailure() {
        if (sListener != null) {
            debugLog("Delivering failure to listener.");
            sListener.OnSelectSnapshotResult(false, mDummyObject, false);
            sListener = null;
        }
    }

    @Override
    protected void deliverSuccess(Intent data) {
        Object snapshot = null;
        boolean isNew = false;

        snapshot = data.getParcelableExtra(Snapshots.EXTRA_SNAPSHOT_METADATA);
        isNew = data.getBooleanExtra(Snapshots.EXTRA_SNAPSHOT_NEW, false);

        if (sListener != null) {
            debugLog("Calling listener.");
            sListener.OnSelectSnapshotResult(true, snapshot, isNew);
            sListener = null;
        }
    }

    @Override
    protected Intent getUiIntent() {
        String title = getIntent().getStringExtra(EXTRA_TITLE);
        boolean allowAddButton = getIntent().getBooleanExtra(EXTRA_ALLOW_ADD, false);
        boolean allowDelete = getIntent().getBooleanExtra(EXTRA_ALLOW_DELETE, false);
        int maxSnapshots = getIntent().getIntExtra(EXTRA_MAX_SNAPSHOTS, 5);

        return Games.Snapshots.getSelectSnapshotIntent(mHelper.getApiClient(), title,
                allowAddButton, allowDelete, maxSnapshots);
    }

    public static void launch(Activity activity, Listener listener, String title,
                              boolean allowAddButton, boolean allowDelete, int maxSnapshots) {
        setListener(listener);
        Intent i = new Intent(activity, SelectSnapshotHelperActivity.class);
        i.putExtra(EXTRA_TITLE, title);
        i.putExtra(EXTRA_ALLOW_ADD, allowAddButton);
        i.putExtra(EXTRA_ALLOW_DELETE, allowDelete);
        i.putExtra(EXTRA_MAX_SNAPSHOTS, maxSnapshots);
        activity.startActivity(i);
    }
}
