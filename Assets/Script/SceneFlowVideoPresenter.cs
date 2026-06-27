using UnityEngine;

using UnityEngine.UI;

using UnityEngine.Video;



/// <summary>

/// Starts video playback for SceneFlow pages (IntroVideo, Alert, etc.).

/// </summary>

public sealed class SceneFlowVideoPresenter : MonoBehaviour

{

    static VideoClip cachedBodyIntroClip;

    static VideoClip cachedDanceIntroClip;



    void OnEnable()

    {

        EnsureKnownPageVideo();

        SceneFlowVideoInstaller.EnsureUiPlayers(transform);

    }



    void OnDisable()

    {

        var players = GetComponentsInChildren<VideoPlayer>(true);

        for (var i = 0; i < players.Length; i++)

        {

            if (players[i] != null && players[i].isPlaying)

                players[i].Stop();

        }

    }



    void EnsureKnownPageVideo()

    {

        if (GetComponentInChildren<VideoPlayer>(true) != null)

            return;



        switch (gameObject.name)

        {

            case "Alert":

                SetupAlertIntroVideo();

                break;

        }

    }



    void SetupAlertIntroVideo()

    {

        var image = FindPrimaryImage();

        if (image == null)

            return;



        var clip = ResolveBodyIntroClip();

        if (clip == null)

        {

            Debug.LogWarning("[SceneFlowVideoPresenter] Body intro clip not found for Alert page.");

            return;

        }



        var host = image.gameObject;

        var vp = host.GetComponent<VideoPlayer>();

        if (vp == null)

            vp = host.AddComponent<VideoPlayer>();



        vp.source = VideoSource.VideoClip;

        vp.clip = clip;

        vp.playOnAwake = false;

        vp.audioOutputMode = VideoAudioOutputMode.Direct;



        if (host.GetComponent<UiVideoPlayer>() == null)

            host.AddComponent<UiVideoPlayer>();



        Debug.Log("[SceneFlowVideoPresenter] Alert page wired to body intro video.");

    }



    Image FindPrimaryImage()

    {

        Image best = null;

        var maxArea = 0f;

        var images = GetComponentsInChildren<Image>(true);

        for (var i = 0; i < images.Length; i++)

        {

            var img = images[i];

            if (img.GetComponent<Button>() != null)

                continue;



            var rect = img.rectTransform;

            var area = rect.rect.width * rect.rect.height;

            if (area > maxArea)

            {

                maxArea = area;

                best = img;

            }

        }



        return best;

    }



    static VideoClip ResolveBodyIntroClip()

    {

        if (cachedBodyIntroClip != null)

            return cachedBodyIntroClip;



        var players = Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (var i = 0; i < players.Length; i++)

        {

            var clip = players[i].clip;

            if (clip == null)

                continue;



            if (clip.name.Contains("바디") || clip.name.Contains("Body"))

            {

                cachedBodyIntroClip = clip;

                return cachedBodyIntroClip;

            }

        }



        return null;

    }



    internal static void CacheIntroClipsFromScene()

    {

        var players = Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (var i = 0; i < players.Length; i++)

        {

            var clip = players[i].clip;

            if (clip == null)

                continue;



            if (cachedBodyIntroClip == null &&

                (clip.name.Contains("바디") || clip.name.Contains("Body")))

                cachedBodyIntroClip = clip;



            if (cachedDanceIntroClip == null &&

                (clip.name.Contains("댄스") || clip.name.Contains("Dance")))

                cachedDanceIntroClip = clip;

        }

    }

}

