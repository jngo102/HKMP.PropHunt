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
            _prop.transform.localPosition = Vector3.zero;
            _prop.transform.rotation = Quaternion.identity;
            _prop.transform.localScale = Vector3.one;
            _propSprite = _prop.AddComponent<SpriteRenderer>();
        }

        public void SetPropSprite(Sprite sprite)
        {
            _propSprite.sprite = sprite;
            
            _prop.transform.localPosition = Vector3.zero;
            _prop.transform.rotation = Quaternion.identity;
            _prop.transform.localScale = Vector3.one;

            if (sprite == null)
            {
                _meshRend.enabled = true;
                _username.SetActive(true);
                return;
            }

            _meshRend.enabled = false;
            _username.SetActive(false);
        }
    }
}