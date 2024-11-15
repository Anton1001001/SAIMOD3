
namespace SAIMOD3;

public class Simulation
{
    private List<Event?> _events = new();
    private float _modelTime;
    private const float TimeEndOfSimulation = 20000f; // Time end of simulation

    private int _countType1; // Counter for type 1 parts
    private int _totalPartsType1;
    private int _countType2; // Counter for type 2 parts
    private int _totalPartsType2;
    private bool _robotFree = true; // Robot is free
    private bool _machine1Free = true; // Machine 1 is free
    private bool _machine2Free = true; // Machine 2 is free
    private int _completedBatches; // Number of completed batches
    private int _totalCompletedBatches;
    private float _totalProcessingTime; // Total processing time for parts
    private int _totalPartsProcessed; // Total number of parts processed
    private const int M1 = 10; // Batch size for type 1
    private const int M2 = 3; // Batch size for type 2

    public float MeanTransportTimeToMachine = 2f; // Transport time to machine
    private const float VarianceTransportTimeToMachine = 1f;

    private const float MeanTransportTimeToExit = 10f; // Transport time to exit conveyor
    private const float VarianceTransportTimeToExit = 2f;
    
    private const float MeanProcessing = 15f;
    private const float VarianceProcessing = 5f;

    private const float MeanExponential = 20f;
    
    private const float DetailProbability = 0.7f;

    public static List<float> AvgMachine1List = new();
    public static List<double> AvgMachine1List_model = new();

    private float _dt;
    private float _avgDetailsMachine1;
    private float _avgDetailsMachine2;
    
    Event? _currentEvent;

    public void Run()
    {
        /*_events.Clear();
    _modelTime = 0;
    _countType1 = 0;
    _countType2 = 0;
    _robotFree = true; // Robot is free
    _machine1Free = true; // Machine 1 is free
    _machine2Free = true; // Machine 2 is free
    _completedBatches = 0;
    _totalPartsType1 = 0;
    _totalPartsType2 = 0;
    _totalProcessingTime = 0;*/
        //_
        
        var time = RandomGenerator.GetExponentialRandom(MeanExponential);
        
        ScheduleEvent(time, EventType.DetailArrival); // Schedule first part arrival

        while (_events.Any() && _modelTime < TimeEndOfSimulation)
        {
            _currentEvent = _events.First();
            _modelTime = _currentEvent!.Time;
            _events.RemoveAt(0);

            switch (_currentEvent.Type)
            {
                case EventType.DetailArrival:
                    HandleDetailArrival();
                    break;
                case EventType.CompleteBatchType1:
                    HandleCompleteBatch(1);
                    break;
                case EventType.CompleteBatchType2:
                    HandleCompleteBatch(2);
                    break;
                case EventType.StartTransportToMachine1:
                    HandleStartTransportToMachine(1);
                    break;
                case EventType.StartTransportToMachine2:
                    HandleStartTransportToMachine(2);
                    break;
                case EventType.FinishTransportToMachine1:
                    HandleFinishTransportToMachine(1);
                    break;
                case EventType.FinishTransportToMachine2:
                    HandleFinishTransportToMachine(2);
                    break;
                case EventType.FinishProcessingMachine1:
                    HandleFinishProcessing(1);
                    break;
                case EventType.FinishProcessingMachine2:
                    HandleFinishProcessing(2);
                    break;
                case EventType.TransportToExit:
                    HandleTransportToExit();
                    break;
                case EventType.TransportToExitEnd:
                    HandleTransportToExitEnd();
                    break;
            }
            AvgMachine1List_model.Add(_avgDetailsMachine1 / _modelTime);
        }
        
        AvgMachine1List.Add(_avgDetailsMachine1/ TimeEndOfSimulation);
       

    }

    private void HandleDetailArrival()
    {
        int detailType = RandomGenerator.GenerateDetailType(DetailProbability);

        if (detailType == 1)
        {
            _countType1++;
            if (_countType1 >= M1 && _robotFree && _machine1Free)
            {
                ScheduleEvent(_modelTime, EventType.CompleteBatchType1);
                _countType1 -= M1;
            }
        }
        else
        {
            _countType2++;
            if (_countType2 >= M2 && _robotFree && _machine2Free)
            {
                ScheduleEvent(_modelTime, EventType.CompleteBatchType2);
                _countType2 -= M2;
            }
        }

        
        _dt = RandomGenerator.GetExponentialRandom(MeanExponential);

        _avgDetailsMachine1 += _countType1 * _dt;
        _avgDetailsMachine2 += _countType2 * _dt;

        ScheduleEvent(_modelTime + _dt, EventType.DetailArrival);
    }

    private void HandleCompleteBatch(int type)
    {
        _robotFree = false;

        ScheduleEvent(_modelTime, type == 1 ? EventType.StartTransportToMachine1 : EventType.StartTransportToMachine2);
    }

    private void HandleStartTransportToMachine(int machineType)
    {
        ScheduleEvent(
            _modelTime + RandomGenerator.GetNormalRandom(MeanTransportTimeToMachine, VarianceTransportTimeToMachine),
            machineType == 1 ? EventType.FinishTransportToMachine1 : EventType.FinishTransportToMachine2);
    }

    private void HandleFinishTransportToMachine(int machineType)
    {
        if (machineType == 1)
        {
            _machine1Free = false;
            var time = RandomGenerator.GetNormalRandom(MeanProcessing, VarianceProcessing);
            _totalProcessingTime += time;
            
            ScheduleEvent(_modelTime + time, EventType.FinishProcessingMachine1);
        }
        else
        {
            _machine2Free = false;
            var time = RandomGenerator.GetNormalRandom(MeanProcessing, VarianceProcessing);
            _totalProcessingTime += time;
            ScheduleEvent(_modelTime + time, EventType.FinishProcessingMachine2);
        }
        _robotFree = true;
    }

    private void HandleFinishProcessing(int machineType)
    {
        _completedBatches++;
        _totalCompletedBatches++;
        _totalPartsProcessed += machineType == 1 ? M1 : M2;
        _totalPartsType1 += machineType == 1 ? M1 : 0;
        _totalPartsType2 += machineType == 2 ? M2 : 0;

        if (machineType == 1)
        {
            _machine1Free = true;
        }
        else
        {
            _machine2Free = true;
        }

        if (_completedBatches >= 3)
        {
            _completedBatches = 0;
            ScheduleEvent(_modelTime, EventType.TransportToExit);
        }
        else
        {
            _robotFree = true;
        }
    }
    private void HandleTransportToExit()
    {
        _robotFree = false; // Robot is busy transporting to exit conveyor
        ScheduleEvent(_modelTime + RandomGenerator.GetNormalRandom(MeanTransportTimeToExit, VarianceTransportTimeToExit), EventType.TransportToExitEnd);
    }

    private void HandleTransportToExitEnd()
    {
        _robotFree = true; // Robot returns and becomes available
    }

    private void ScheduleEvent(float time, EventType type)
    {
        _events.Add(new Event(time, type));
        _events = _events.OrderBy(e => e.Time).ToList();
    }

}