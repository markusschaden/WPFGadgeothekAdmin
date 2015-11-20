using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ch.hsr.wpf.gadgeothek.domain;

namespace WPFGadgeothekAdmin.viewmodel
{
    public class CustomerViewModel
    {
        public Customer Customer { get; set; }
        public List<Loan> Loans { get; set; }

        public bool HasOverdue
        {
            get
            {
                return Loans.Count(l => l.WasReturned == false && l.IsOverdue) > 0;
            }
        }
    }
}
