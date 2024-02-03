// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SampleApp;

#nullable disable

public class DataGridDataItem : INotifyDataErrorInfo, IComparable, INotifyPropertyChanged
{
    private Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();
    private uint _rank;
    private string _mountain;
    private uint _height;
    private string _range;
    private string _parentMountain;
    private string coordinates;
    private uint prominence;
    private DateTimeOffset first_ascent;
    private string ascents;

    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    public event PropertyChangedEventHandler PropertyChanged;

    public uint Rank
    {
        get
        {
            return _rank;
        }

        set
        {
            if (_rank != value)
            {
                _rank = value;
                OnPropertyChanged();
            }
        }
    }

    public string Mountain
    {
        get
        {
            return _mountain;
        }

        set
        {
            if (_mountain != value)
            {
                _mountain = value;

                bool isMountainValid = !_errors.ContainsKey("Mountain");
                if (_mountain == string.Empty && isMountainValid)
                {
                    List<string> errors = new List<string>();
                    errors.Add("Mountain name cannot be empty");
                    _errors.Add("Mountain", errors);
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs("Mountain"));
                }
                else if (_mountain != string.Empty && !isMountainValid)
                {
                    _errors.Remove("Mountain");
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs("Mountain"));
                }

                OnPropertyChanged();
            }
        }
    }

    public uint Height_m
    {
        get
        {
            return _height;
        }

        set
        {
            if (_height != value)
            {
                _height = value;
                OnPropertyChanged();
            }
        }
    }

    public string Range
    {
        get
        {
            return _range;
        }

        set
        {
            if (_range != value)
            {
                _range = value;

                bool isRangeValid = !_errors.ContainsKey("Range");
                if (_range == string.Empty && isRangeValid)
                {
                    List<string> errors = new List<string>();
                    errors.Add("Range name cannot be empty");
                    _errors.Add("Range", errors);
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs("Range"));
                }
                else if (_range != string.Empty && !isRangeValid)
                {
                    _errors.Remove("Range");
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs("Range"));
                }

                OnPropertyChanged();
            }
        }
    }

    public string Parent_mountain
    {
        get
        {
            return _parentMountain;
        }

        set
        {
            if (_parentMountain != value)
            {
                _parentMountain = value;

                bool isParentValid = !_errors.ContainsKey("Parent_mountain");
                if (_parentMountain == string.Empty && isParentValid)
                {
                    List<string> errors = new List<string>();
                    errors.Add("Parent_mountain name cannot be empty");
                    _errors.Add("Parent_mountain", errors);
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs("Parent_mountain"));
                }
                else if (_parentMountain != string.Empty && !isParentValid)
                {
                    _errors.Remove("Parent_mountain");
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs("Parent_mountain"));
                }

                OnPropertyChanged();
            }
        }
    }

    public string Coordinates
    {
        get => coordinates;
        set
        {
            if (coordinates != value)
            {
                coordinates = value;
                OnPropertyChanged();
            }
        }
    }

    public uint Prominence
    {
        get => prominence;
        set
        {
            if (prominence != value)
            {
                prominence = value;
                OnPropertyChanged();
            }
        }
    }

    // You need to use DateTimeOffset to get proper binding to the CalendarDatePicker control, DateTime won't work.
    public DateTimeOffset First_ascent
    {
        get => first_ascent; set
        {
            if (first_ascent != value)
            {
                first_ascent = value;
                OnPropertyChanged();
            }
        }
    }

    public string Ascents
    {
        get => ascents; set
        {
            if (ascents != value)
            {
                ascents = value;
                OnPropertyChanged();
            }
        }
    }

    bool INotifyDataErrorInfo.HasErrors
    {
        get
        {
            return _errors.Keys.Count > 0;
        }
    }

    IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
    {
        if (propertyName == null)
        {
            propertyName = string.Empty;
        }

        if (_errors.ContainsKey(propertyName))
        {
            return _errors[propertyName];
        }
        else
        {
            return null;
        }
    }

    int IComparable.CompareTo(object obj)
    {
        int lnCompare = Range.CompareTo((obj as DataGridDataItem).Range);

        if (lnCompare == 0)
        {
            return Parent_mountain.CompareTo((obj as DataGridDataItem).Parent_mountain);
        }
        else
        {
            return lnCompare;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}