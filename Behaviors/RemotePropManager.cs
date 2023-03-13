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
        }

        /// <summary>
        /// Set the remote player's prop's sprite.
        /// </summary>
        /// <param name="sprite">The sprite to change to</param>
        public void SetPropSprite(Sprite sprite)
        {
            PropHunt.Instance.Log("Sprite name to change to: " + sprite?.name);
            PropHunt.Instance.Log("Current prop sprite name: " + _propSprite?.sprite?.name);
            if (sprite?.name == _propSprite?.sprite?.name) return;

            PropHunt.Instance.Log("Prop sprite null? " + (_propSprite == null));
            
            _propSprite.sprite = sprite;

            PropHunt.Instance.Log("Prop sprite is now: " + _propSprite.sprite?.name);

            ResetPropTransform();

            if (sprite == null)
            {
                PropHunt.Instance.Log("Sprite is null, showing player");
                _meshRend.enabled = true;
                _username.SetActive(true);
                return;
            }

            PropHunt.Instance.Log("Sprite is NOT null, hiding player");
            _meshRend.enabled = false;
            if (HeroController.instance.GetComponent<LocalPropManager>()?.PropSprite != null)
            {
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