using System.Collections;
using UnityEngine;

namespace PropHunt.Behaviors
{
    [RequireComponent(typeof(MeshRenderer))]
    internal class RemotePropManager : MonoBehaviour
    {
        private MeshRenderer _meshRend;
        private GameObject _username;
        private GameObject _prop;
        private SpriteRenderer _propSprite;

        private void Awake()
        {
            _meshRend = GetComponent<MeshRenderer>();
            _username = transform.parent.Find("Username").gameObject;
            _prop = new GameObject("Prop");
            _prop.transform.SetParent(transform);
            ResetPropTransform();
            _propSprite = _prop.AddComponent<SpriteRenderer>();
            PropHunt.Instance.Log("RemotePropManager Awake");
        }

        /// <summary>
        /// Set the remote player's prop's sprite.
        /// </summary>
        /// <param name="sprite">The sprite to change to</param>
        public IEnumerator SetPropSprite(Sprite sprite)
        {
            yield return new WaitUntil(() => _propSprite != null);
            
            PropHunt.Instance.Log("Setting prop sprite to " + sprite?.name);
            PropHunt.Instance.Log("Prop SpriteRenderer null? " + (_propSprite == null));
            _propSprite.sprite = sprite;

            PropHunt.Instance.Log("Resetting prop transform");
            ResetPropTransform();

            if (sprite == null)
            {
                PropHunt.Instance.Log("Sprite is null, showing player");
                _meshRend.enabled = true;
                _username.SetActive(true);
                yield break;
            }

            PropHunt.Instance.Log("Disabling meshrend");
            _meshRend.enabled = false;
            if (!HeroController.instance.GetComponent<LocalPropManager>().enabled)
            {
                PropHunt.Instance.Log("Disabling username");
                _username.SetActive(false);
            }
        }

        /// <summary>
        /// Reset the prop's transform to default.
        /// </summary>
        private void ResetPropTransform()
        {
            _prop.transform.localPosition = Vector3.zero;
            _prop.transform.rotation = Quaternion.identity;
            _prop.transform.localScale = Vector3.one;
        }
    }
}