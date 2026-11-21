using System;
using System.Collections.Generic;
using CollinGame.Core;
using UnityEngine;
using Yarn.Unity;

namespace CollinGame.Kits.VisualNovel
{
    /// <summary>
    /// Keeps course Int Variables in step with number variables in the Yarn
    /// script, such as $ren_affection. That way the rest of the game can use
    /// them: a UI Variable Binder can show them, an Int Variable Condition can
    /// react to them, and the Relationship Meters can draw them.
    ///
    /// The Yarn script is in charge: at the start, each Int Variable takes the
    /// Yarn variable's starting value (from its &lt;&lt;declare&gt;&gt;). With Two Way
    /// ticked, changes made by the game are sent back to the script too.
    /// </summary>
    [AddComponentMenu("Collin Game/Visual Novel Kit/Yarn Variable Link")]
    public class YarnVariableLink : MonoBehaviour
    {
        [Serializable]
        public class Link
        {
            [Tooltip("The Yarn variable, with its $, such as $ren_affection. It must be a number.")]
            public string yarnVariable = "$affection";

            [Tooltip("The course Int Variable to keep in step.")]
            public IntVariable variable;
        }

        [Tooltip("Optional. The Dialogue Runner. Found in the scene if empty.")]
        [SerializeField] DialogueRunner runner;

        [Tooltip("The variables to keep in step.")]
        [SerializeField] List<Link> links = new List<Link>();

        [Tooltip("Ticked: when the game changes an Int Variable, the Yarn variable changes too.")]
        [SerializeField] bool twoWay;

        readonly List<IDisposable> yarnListeners = new List<IDisposable>();
        readonly List<(IntVariable variable, Action<int> handler)> gameListeners = new List<(IntVariable, Action<int>)>();
        bool copying;

        void Start()
        {
            if (runner == null)
            {
                runner = FindFirstObjectByType<DialogueRunner>();
            }

            VariableStorageBehaviour storage = runner != null ? runner.VariableStorage : null;

            if (storage == null)
            {
                Debug.LogWarning($"Yarn Variable Link on \"{name}\": there is no Dialogue Runner with variable storage in the scene.", this);
                return;
            }

            foreach (Link link in links)
            {
                if (link == null || link.variable == null || string.IsNullOrEmpty(link.yarnVariable))
                {
                    continue;
                }

                string yarnName = link.yarnVariable.StartsWith("$") ? link.yarnVariable : "$" + link.yarnVariable;
                IntVariable variable = link.variable;

                if (storage.TryGetValue(yarnName, out float start))
                {
                    CopyToGame(variable, start);
                }

                try
                {
                    yarnListeners.Add(storage.AddChangeListener<float>(yarnName, value => CopyToGame(variable, value)));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Yarn Variable Link on \"{name}\": can't follow {yarnName}. Declare it in the Yarn script as a number, such as <<declare {yarnName} = 0>>. ({e.Message})", this);
                    continue;
                }

                if (twoWay)
                {
                    Action<int> handler = value =>
                    {
                        if (!copying)
                        {
                            storage.SetValue(yarnName, (float)value);
                        }
                    };

                    variable.Changed += handler;
                    gameListeners.Add((variable, handler));
                }
            }
        }

        void OnDestroy()
        {
            foreach (IDisposable listener in yarnListeners)
            {
                listener.Dispose();
            }

            foreach ((IntVariable variable, Action<int> handler) in gameListeners)
            {
                variable.Changed -= handler;
            }

            yarnListeners.Clear();
            gameListeners.Clear();
        }

        void CopyToGame(IntVariable variable, float value)
        {
            copying = true;
            variable.SetValue(Mathf.RoundToInt(value));
            copying = false;
        }
    }
}
