using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Utility
{
    public class IgnoreEventsRegion
    {
        private class RegionState
        {
            public bool IsActive;
        }

        private class RegionStateTracker : IDisposable
        {
            private RegionState _state;

            public RegionStateTracker(RegionState state)
            {
                if (state.IsActive)
                    throw new InvalidOperationException("The region is already active!");

                _state = state;
                _state.IsActive = true;
            }

            public void Dispose()
            {
                if (_state != null)
                {
                    _state.IsActive = false;
                }
                _state = null;
            }
        }

        private readonly RegionState _state;

        public IgnoreEventsRegion()
        {
            _state = new RegionState();
        }

        public IDisposable Start()
        {
            return new RegionStateTracker(_state);
        }

        public bool IsActive
        {
            get { return _state.IsActive; }
        }
    }
}
