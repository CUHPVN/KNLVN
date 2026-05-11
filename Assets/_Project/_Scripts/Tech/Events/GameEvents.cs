namespace Tech.Events 
{
    public struct HealthChangedEvent
    {
        public float CurrentHealth;
        public float MaxHealth;
    }
    public struct GameOverEvent { }

    public struct DataLoadedEvent
    {
        public string JsonData;
    }
    /*
    public struct OnSlotRollComplete {
        public bool isCenter;
    }
    private void OnEnable()
    {
        EventBus.Instance.Subscribe<OnSlotRollComplete>(OnSlotRollComplete);
    }
    private void OnSlotRollComplete(OnSlotRollComplete evt)
    {
        Debug.Log("Complete");
    }
        EventBus.Instance.Publish(new OnSlotRollComplete());

     */
}