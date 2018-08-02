using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

#region Helper Classes
public class Replay
{
    public List<double> States { get; set; }
    public double Reward { get; set; }

    public Replay(double distanceToTop, double distanceToBottom, double reward)
    {
        States = new List<double>
        {
            distanceToTop,
            distanceToBottom,
        };
        Reward = reward;
    }
}
#endregion

public class QLearningBrain : MonoBehaviour
{
    #region Fields
    [SerializeField] 
    private GameObject _stats;
    // Make sure the value is large enough to achieve success.
    [SerializeField] 
    private float _verticalSpeedMultiplyer = 50f;
    [SerializeField]
    private float _timeScale = 1f;
    private Text[] _statsTexts;
    private Vector2 _startPosition;
    private bool _isAlive = true;
    private Senses _senses = null;
    private Rigidbody2D _rigidbody2D;

    private Ann _ann;
    // List of past actions and rewards.
    private List<Replay> _replayMemory = new List<Replay>();

    // Reward to associate with actions.
    private float _reward = 0f;
    private int _memoryCapacity = 10000;
    // How much future states affect rewards.
    private float _discount = 0.99f;

    // Chance of picking random action.
    [SerializeField] 
    private bool _useExploration = false;
    [SerializeField] 
    private float _exploreRate = 100f;
    private float _maxExploreRate = 100f;
    private float _minExploreRate = 0.01f;
    // Decay amount for each update.
    private float _exploreDecay = 0.0001f;

    // How many times a wall is hit.
    private int _failCount = 0;
    private float _timer = 0f;
    private float _maxBalanceTime = 0f;
    #endregion

    // Use this for initialization
    private void Start()
    {
        _ann = new Ann(2, 2, 1, 6, 0.2);

        _statsTexts = _stats.GetComponentsInChildren<Text>();
        Assert.IsNotNull(_statsTexts);

        _senses = GetComponent<Senses>();
        Assert.IsNotNull(_senses);

        _rigidbody2D = GetComponent<Rigidbody2D>();
        Assert.IsNotNull(_rigidbody2D);

        _startPosition = transform.position;

        Time.timeScale = _timeScale;
    }

    private void FixedUpdate()
    {
        _timer += Time.deltaTime;

        _senses.CheckForObstacle();

        var states = new List<double>
        {
            _senses.DistanceToTop,
            _senses.DistanceToBottom
        };

        var qValues = new List<double>();
        qValues = SoftMax(_ann.CalcOutput(states));

        double maxQValue = qValues.Max();
        int maxQValueIndex = qValues.ToList().IndexOf(maxQValue);

        // Explore.
        _exploreRate = Mathf.Clamp(_exploreRate - _exploreDecay, _minExploreRate, _maxExploreRate);
        if ((Random.Range(0, 100) < _exploreRate) && (_useExploration == true))
        {
            maxQValueIndex = Random.Range(0, 2);
        }

        // Apply force based on chosen maxQValue.
        if (maxQValueIndex == 0)
        {
            // Apply up force.
            _rigidbody2D.AddForce(transform.up * (float)qValues[maxQValueIndex] * _verticalSpeedMultiplyer);
        }
        else if (maxQValueIndex == 1)
        {
            // Apply down force.
            _rigidbody2D.AddForce(-transform.up * (float)qValues[maxQValueIndex] * _verticalSpeedMultiplyer);
        }

         //Reward based on the state of the bird.
        if (!_isAlive)
        {
            _reward = -1.0f;
        }
        else
        {
            _reward = 0.1f;
        }

        // Set up replay memory.
        var lastMemory = new Replay(
            _senses.DistanceToTop,
            _senses.DistanceToBottom,
            _reward);

        // Ensure _memoryCapacity is not exceeded.
        if (_replayMemory.Count > _memoryCapacity)
        {
            _replayMemory.RemoveAt(0);
        }

        _replayMemory.Add(lastMemory);

        // Execute Q-Learning training when bird collided.
        if (!_isAlive)
        {
            TrainQLearning(maxQValue);
        }
    }

    private void TrainQLearning(double maxQValue)
    {
        Debug.Log("TrainQLearning");

        for (int i = _replayMemory.Count - 1; i >= 0; i--)
        {
            var currentTrainingOutputs = new List<double>();
            currentTrainingOutputs = SoftMax(_ann.CalcOutput(_replayMemory[i].States));

            double currentMaxQ = currentTrainingOutputs.Max();
            // Best action according to the current memories.
            int action = currentTrainingOutputs.ToList().IndexOf(currentMaxQ);

            double feedback = 0d;
            var nextTrainingOutputs = new List<double>();

            // Last memory in list OR memory has -1 reward (ball dropped).
            if (i == _replayMemory.Count - 1 || _replayMemory[i].Reward == -1)
            {
                feedback = _replayMemory[i].Reward;
            }
            else
            {
                nextTrainingOutputs = SoftMax(_ann.CalcOutput(_replayMemory[i + 1].States));
                maxQValue = nextTrainingOutputs.Max();
                // Bellman equasion.
                feedback = _replayMemory[i].Reward + _discount * maxQValue;
            }

            currentTrainingOutputs[action] = feedback;
            _ann.Train(_replayMemory[i].States, currentTrainingOutputs);
        }

        UpdateMaxBalanceTime();
        ResetOnFail();
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateStats();
    }

    private void ResetOnFail()
    {
        //transform.rotation = Quaternion.identity;
        ResetPosition();
        _replayMemory.Clear();
        _failCount++;
        _isAlive = true;
    }

    private void UpdateMaxBalanceTime()
    {
        if (_timer > _maxBalanceTime)
        {
            _maxBalanceTime = _timer;
        }
        _timer = 0;
    }

    private void UpdateStats()
    {
        _statsTexts[0].text = "Fails: " + _failCount;
        _statsTexts[1].text = "Explore rate: " + _exploreRate;
        _statsTexts[2].text = "Best balance time: " + _maxBalanceTime;
        _statsTexts[3].text = "Current balance time: " + _timer;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown("space"))
        {
            transform.position = _startPosition;
        }
    }

    /// <summary>
    /// Normalizes all values to a range between 0 and 1,
    /// so that all values add up to 1.
    /// </summary>
    /// <returns>A normalized list of values that adds up to 1.</returns>
    /// <param name="values">Values to normalize (here: outputs of the NN).</param>
    private List<double> SoftMax(List<double> values)
    {
        double max = values.Max();

        float scale = 0f;
        for (int i = 0; i < values.Count; i++)
        {
            scale += Mathf.Exp((float)(values[i] - max));
        }

        var result = new List<double>();
        for (int i = 0; i < values.Count; i++)
        {
            result.Add(Mathf.Exp((float)(values[i] - max)) / scale);
        }
        return result;
    }

    private void ResetPosition()
    {
        transform.position = _startPosition;
        _rigidbody2D.velocity = Vector2.zero;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "top" || collision.gameObject.tag == "bottom")
        {
            Debug.Log("Collided");
            _isAlive = false;
        }
    }
}
