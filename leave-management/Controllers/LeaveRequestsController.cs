using AutoMapper;
using leave_management.Contracts;
using leave_management.Data;
using leave_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace leave_management.Controllers
{
    [Authorize]
    public class LeaveRequestsController : Controller
    {
        private readonly IUnitofWork _unitofWork;
        private readonly IMapper _mapper;
        private readonly UserManager<Employee> _userManager;

        public LeaveRequestsController(IUnitofWork unitofWork, 
         IMapper mapper, UserManager<Employee> userManager)
        {
            _unitofWork = unitofWork;
            _mapper = mapper;
            _userManager = userManager;
        }

        [Authorize(Roles = "Administrator")]
        // GET: LeaveRequestsController
        public async Task<ActionResult> Index()
        {
            //var leaveRequests = await _leaverequestrepo.FindAll();
            var leaveRequests = await _unitofWork.LeaveRequests.FindAll();
            var leaveRequestModel = _mapper.Map<List<LeaveRequestVM>>(leaveRequests);
            var model = new AdminLeaveRequestViewVM
            {
                TotalRequests = leaveRequestModel.Count,
                ApprovedRequests = leaveRequestModel.Count(q => q.Approved == true),
                PeandingRequests = leaveRequestModel.Count(q => q.Approved == null),
                RejectedRequests = leaveRequestModel.Count(q => q.Approved == false),
                LeaveRequests = leaveRequestModel
            };
            return View(model);
        }

        // GET: LeaveRequestsController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            //var leaveRequest = await _leaverequestrepo.FindById(id);
            var leaveRequest = await _unitofWork.LeaveRequests.Find(q => q.Id == id,
                includes: new List<string> {"ApprovedBy","RequestingEmployee","LeaveType" });
            var model = _mapper.Map<LeaveRequestVM>(leaveRequest);
            return View(model);
        }

        public async Task<ActionResult> ApproveRequest(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
               // var leaveRequest = await _leaverequestrepo.FindById(id);
                var leaveRequest = await _unitofWork.LeaveRequests.Find(q => q.Id == id);
                var employeeid = leaveRequest.RequestingEmployeeId;
                var leavetypeid = leaveRequest.LeaveTypeId;
                var period = DateTime.Now.Year;

               //var allocation = await _leaveallocationrepo.GetLeaveAllocationsByEmployeeAndType(employeeid, leavetypeid);
               var allocation = await _unitofWork.LeaveAllocations.Find(q => q.EmployeeId == employeeid 
                        && q.Period == period && q.LeaveTypeId == leavetypeid);

                int daysRequested = (int)(leaveRequest.EndDate.Date - leaveRequest.StartDate.Date).TotalDays;
                allocation.NumberofDays -= daysRequested;
                leaveRequest.Approved = true;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

                //var isSuccess = _leaverequestrepo.Update(leaveRequest);
                //await _leaveallocationrepo.Update(allocation);

                _unitofWork.LeaveRequests.Update(leaveRequest);
                _unitofWork.LeaveAllocations.Update(allocation);

                await _unitofWork.Save();

                return RedirectToAction(nameof(Index));
                
            }
            catch(Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
            
        }

        public async Task<ActionResult> RejectRequest(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
               // var leaveRequest = await _leaverequestrepo.FindById(id);
                var leaveRequest = await _unitofWork.LeaveRequests.Find(q => q.Id == id);

                leaveRequest.Approved = false;
                leaveRequest.ApprovedById = user.Id;
                leaveRequest.DateActioned = DateTime.Now;

                //await _leaverequestrepo.Update(leaveRequest);
                _unitofWork.LeaveRequests.Update(leaveRequest);
                await _unitofWork.Save();
                

                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: LeaveRequestsController/Create
        public async Task<ActionResult> Create()
        {
            //var leaveTypes = await _leavetyperepo.FindAll();
            var leaveTypes = await _unitofWork.LeaveTypes.FindAll();
            var leaveTypeItems = leaveTypes.Select(q => new SelectListItem
            {
                Text = q.Name,
                Value = q.Id.ToString()
            });
            var model = new CreateLeaveRequestVM
            {
                LeaveTypes = leaveTypeItems
            };
            return View(model);
        }

        // POST: LeaveRequestsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(CreateLeaveRequestVM model)
        {
            try
            {
                var startDate = Convert.ToDateTime(model.StartDate);
                var endDate = Convert.ToDateTime(model.EndDate);
                var leaveTypes = await _unitofWork.LeaveTypes.FindAll();
                var leaveTypeItems = leaveTypes.Select(q => new SelectListItem
                {
                    Text = q.Name,
                    Value = q.Id.ToString()
                });
                model.LeaveTypes = leaveTypeItems;
                if (!ModelState.IsValid || (DateTime.Compare(startDate,endDate) > 1 ))
                {
                    return View(model);
                }

                var employee = await _userManager.GetUserAsync(User);
                //var allocation = await _leaveallocationrepo.GetLeaveAllocationsByEmployeeAndType(employee.Id,model.LeaveTypeId);
                var period = DateTime.Now.Year;

                //var allocation = await _leaveallocationrepo.GetLeaveAllocationsByEmployeeAndType(employeeid, leavetypeid);
                var allocation = await _unitofWork.LeaveAllocations.Find(q => q.EmployeeId == employee.Id
                         && q.Period == period && q.LeaveTypeId == model.LeaveTypeId);

                int daysRequested = (int)(endDate.Date - startDate.Date).TotalDays;

                if(daysRequested > allocation.NumberofDays)
                {
                    ModelState.AddModelError("", "You do not have sufficient days for this Request");
                    return View(model);
                }

                var leaveRequestModel = new LeaveRequestVM
                {
                    RequestingEmployeeId = employee.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    Approved = null,
                    DateRequested = DateTime.Now,
                    DateActioned = DateTime.Now,
                    LeaveTypeId = model.LeaveTypeId,
                    RequestComments = model.RequestComments

                };

                var leaveRequest = _mapper.Map<LeaveRequest>(leaveRequestModel);
                //var isSuccess = await _leaverequestrepo.Create(leaveRequest);
                
                await _unitofWork.LeaveRequests.Create(leaveRequest);
                await _unitofWork.Save();

                return RedirectToAction(nameof(MyLeave));
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("", "Something went wrong");
                return View();
            }
        }

        public async Task<ActionResult> MyLeave()
        {
            var employee = await _userManager.GetUserAsync(User);
            var employeeid = employee.Id;

            //var employeeAllocation = await _leaveallocationrepo.GetLeaveAllocationsByEmployee(employeeid);
            var employeeAllocation = await _unitofWork.LeaveAllocations.FindAll(q=> q.EmployeeId==employeeid,
                includes: new List<string> {"LeaveType"});
            //var employeeRequest = await _leaverequestrepo.GetLeaveRequestsByEmployee(employeeid);
            var employeeRequest = await _unitofWork.LeaveRequests.FindAll(q => q.RequestingEmployeeId == employeeid);

            var employeeAllocationModel = _mapper.Map<List<LeaveAllocationVM>>(employeeAllocation);
            var employeeRequestModel = _mapper.Map<List<LeaveRequestVM>>(employeeRequest);

            var model = new EmployeeLeaveRequestViewVM
            {
                LeaveAllocations = employeeAllocationModel,
                LeaveRequests = employeeRequestModel
            };
            return View(model);
        }

        public async Task<ActionResult> CancelRequest(int id)
        {
            var leaveRequest = await _unitofWork.LeaveRequests.Find(q => q.Id == id);
            leaveRequest.Cancelled = true;
            _unitofWork.LeaveRequests.Update(leaveRequest);
            await _unitofWork.Save();
            return RedirectToAction("MyLeave");
        }
        // GET: LeaveRequestsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: LeaveRequestsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: LeaveRequestsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveRequestsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        protected override void Dispose(bool disposing)
        {
            _unitofWork.Dispose();
            base.Dispose(disposing);
        }
    }
}
