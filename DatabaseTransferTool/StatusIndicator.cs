using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseTransferTool {

    /// <summary>
    /// A status indicator class to act as a model for a ProgressBar view
    /// </summary>
    public class StatusIndicator {


        private int minimum = 0;
        private int maximum = 0;
        private int value = 0;
        private int step = 0;

        public int Minimum { 
            get { 
                return minimum; 
            }
            set {
                minimum = value;
                UpdateObservers();
            }
        }

        public int Maximum { 
            get { 
                return maximum; 
            }
            set {
                maximum = value;
                UpdateObservers();
            }
        }

        public int Value {
            get {
                return value;
            }
            set {
                this.value = value;
                UpdateObservers();
            }
        }

        /// <summary>
        /// The amount by which to move progress forward/backward when PerformStep is invoked
        /// </summary>
        public int Step {
            get {
                return step;
            }
            set {
                step = value;
                UpdateObservers();
            }
        }

        public event EventHandler Update;

        public StatusIndicator() {
            Minimum = 0;
            Maximum = 0;
            Value = 0;
            Step = 0;
        }

        private void UpdateObservers() {
            EventHandler handler = Update;
            
            if (handler != null) {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Move the progress mark ahead a step and notify the observers
        /// </summary>
        public void PerformStep() {

            if (Value + Step >= Maximum) {
                Value = Maximum;
            }
            else if (Value + Step <= Minimum) {
                Value = Minimum;
            }
            else {
                Value += Step;
            }

            UpdateObservers();

        }

    }
}
