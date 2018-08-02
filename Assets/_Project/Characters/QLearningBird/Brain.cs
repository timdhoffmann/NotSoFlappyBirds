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

    public Replay(double zRotation, double ballPositionX, double ballVelocityX, double reward)
    {
        States = new List<double>
        {
            zRotation,
            ballPositionX,
            ballVelocityX
        };
        Reward = reward;
    }
}
#endregion

public class Brain : MonoBehaviour
{
    #region Fields
    [SerializeField] private GameObject _ball;
    [SerializeField] private GameObject _stats;
    private Text[] _statsTexts;
    private Vector3 _ballStartPosition;

    private Ann _ann;
    // List of past actions and rewards.
    private List<Replay> _replayMemory = new List<Replay>();

    // Reward to associate with actions.
    private float _reward = 0f;
    private int _memoryCapacity = 10000;
    // How much future states affect rewards.
    private float _discount = 0.99f;

    // Chance of picking random action.
    [SerializeField] private bool _useExploration = false;
    [SerializeField] private float _exploreRate = 100f;
    private float _maxExploreRate = 100f;
    private float _minExploreRate = 0.01f;
    // Decay amount for each update.
    private float _exploreDecay = 0.0001f;

    // How many times the ball is dropped.
    private int _failCount = 0;
    private float _timer = 0f;
    private float _maxBalanceTime = 0f;

    // Max angle to apply to tilting each update.
    // Make sure the value is large enough to achieve success.
    private float _tiltSpeed = 0.5f;
    #endregion

    // Use this for initialization
    private void Start()
    {
        _ann = new Ann(3, 2, 1, 6, 0.2);

        _statsTexts = _stats.GetComponentsInChildren<Text>();
        Assert.IsNotNull(_statsTexts);

        Assert.IsNotNull(_ball);
        _ballStartPosition = _ball.transform.position;

        Time.timeScale = 5.0f;
    }

    // Update is called once per frame
    private void Update()
    {
        UpdateStats();
    }

    private void FixedUpdate()
    {
        _timer += Time.deltaTime;

        var states = new List<double>
        {
            transform.rotation.z,
            _ball.transform.position.x,
            _ball.GetComponent<Rigidbody>().angularVelocity.z // TODO: Double check axis!
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

        // TOTO: Check case for 0 and 1 and when it relates to left/right.
        // Rotate based on chosen maxQValue.
        if (maxQValueIndex == 0)
        {
            // Rotate to the right.
            transform.Rotate(Vector3.forward, _tiltSpeed * (float)qValues[maxQValueIndex]);
        }
        else if (maxQValueIndex == 1)
        {
            // Rotate to the left.
            transform.Rotate(Vector3.forward, -_tiltSpeed * (float)qValues[maxQValueIndex]);
        }

        // Reward based on the state of the ball.
        Assert.IsNotNull(_ball.GetComponent<BallState>());
        if (_ball.GetComponent<BallState>().Dropped)
        {
            _reward = -1.0f;
        }
        else
        {
            _reward = 0.1f;
        }

        // Set up replay memory.
        var lastMemory = new Replay(
            transform.rotation.z,
            _ball.transform.position.x,
            _ball.GetComponent<Rigidbody>().angularVelocity.x,
            _reward);

        // Ensure _memoryCapacity is not exceeded.
        if (_replayMemory.Count > _memoryCapacity)
        {
            _replayMemory.RemoveAt(0);
        }

        _replayMemory.Add(lastMemory);

        // Execute Q-Learning training when ball is dropped.
        if (_ball.GetComponent<BallState>().Dropped)
        {
            TrainQLearning(maxQValue);
        }
    }

    private void TrainQLearning(double maxQValue)
    {
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

    private void ResetOnFail()
    {
        _ball.GetComponent<BallState>().Dropped = false;
        transform.rotation = Quaternion.identity;
        ResetBall();
        _replayMemory.Clear();
        _failCount++;
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
            _ball.transform.position = _ballStartPosition;
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

    private void ResetBall()
    {
        _ball.transform.position = _ballStartPosition;
        _ball.GetComponent<Rigidbody>().velocity = Vector3.zero;
        _ball.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }
}
