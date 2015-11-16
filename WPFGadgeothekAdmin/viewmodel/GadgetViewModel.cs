using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ch.hsr.wpf.gadgeothek.domain;

namespace WPFGadgeothekAdmin.viewmodel
{
    public class GadgetViewModel
    {
        public Gadget Gadget { get; set; }
        public List<Loan> Loans{ get; set; }
        public DateTime? ReturnDate
        {
            get
            {
                if (!IsAvailable)
                {
                    return Loans.Find(l => l.WasReturned == false).ReturnDate;
                }
                else
                {
                    return null;
                }
            }
        }
        public DateTime? PickupDate
        {
            get {
                if (!IsAvailable)
                {
                    return Loans.Find(l => l.WasReturned == false).PickupDate;
                }
                else
                {
                    return null;
                }
            }
        }
        public bool IsAvailable
        {
            get
            {
                return Loans.Count(l => l.WasReturned == false) == 0;
            }
        }

    }
}
