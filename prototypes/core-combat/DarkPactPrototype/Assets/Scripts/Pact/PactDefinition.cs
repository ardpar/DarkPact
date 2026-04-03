using UnityEngine;

namespace DarkPact.Core
{
    public enum PactId { Katliam, KanKalkani, GolgeAdimi, LanetliDokunus, Acgozluluk }

    [CreateAssetMenu(fileName = "NewPact", menuName = "DarkPact/Pact Definition")]
    public class PactDefinition : ScriptableObject
    {
        public PactId Id;
        public string PactName;
        [TextArea] public string BoonDescription;
        [TextArea] public string BaneDescription;
        public Sprite Icon;
        public Color PactColor = Color.magenta;
    }
}
