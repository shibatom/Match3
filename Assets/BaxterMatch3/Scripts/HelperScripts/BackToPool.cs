using Spine.Unity;
using Internal.Scripts;
using Internal.Scripts.System.Pool;
using UnityEngine;

namespace HelperScripts
{
    public class BackToPool : MonoBehaviour
    {
        public bool DoesHaveAnimation = false;
        public Animation _animation;
        public bool DoesHaveSpines;

        [SpineAnimation(dataField: "_spineAnimation1")]
        public string _animation1;

        [SpineAnimation(dataField: "_spineAnimation2")]
        public string _animation2;


        public Spine.Unity.SkeletonAnimation _spineAnimation1;
        public Spine.Unity.SkeletonAnimation _spineAnimation2;


        public void StartAnimation()
        {
            if (DoesHaveAnimation)
            {
                _animation?.Play();
            }

            if (DoesHaveSpines)
            {
                if (_spineAnimation1 != null)
                {
                    Spine.TrackEntry trackEntry1 = _spineAnimation1.AnimationState.SetAnimation(0, _animation1, false);
                    if (trackEntry1 != null)
                    {
                        trackEntry1.Complete += (entry) => BackToPoolNow();
                    }
                }

                if (_spineAnimation2 != null)
                {
                    Spine.TrackEntry trackEntry2 = _spineAnimation2.AnimationState.SetAnimation(0, _animation2, false);
                    //  if (trackEntry2 != null)
                    // {
                    //     trackEntry2.Complete += (entry) => BackToPoolNow();
                    // }
                }
            }
        }

        void OnDisable()
        {
            if (DoesHaveSpines)
            {
                _spineAnimation1?.AnimationState.ClearTracks();
                _spineAnimation2?.AnimationState.ClearTracks();
            }

            if (transform.rotation.z != 0)
            {
                transform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }

        public void BackToPoolNow()
        {
            if (gameObject.name.Contains("StripesEffect"))
                MainManager.Instance.EndBusyOperation();

            ObjectPoolManager.Instance.PutBack(gameObject);
            //gameObject.SetActive(false);
        }
    }
}