using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using PersonalFinance.Services;

namespace AtomFinance.WinUI.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected readonly IFinanceService FinanceService;
    private readonly DispatcherQueue _dispatcherQueue;

    protected ViewModelBase(IFinanceService financeService)
    {
        FinanceService = financeService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        FinanceService.DataChanged += OnDataChangedInternal;
    }

    private void OnDataChangedInternal(object? sender, EventArgs e)
    {
        if (_dispatcherQueue != null && !_dispatcherQueue.HasThreadAccess)
        {
            _dispatcherQueue.TryEnqueue(OnDataRefreshed);
        }
        else
        {
            OnDataRefreshed();
        }
    }

    protected virtual void OnDataRefreshed()
    {
    }
}
