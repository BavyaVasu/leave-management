using AutoMapper;
using leave_management.Contracts;
using leave_management.Data;
using leave_management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace leave_management.Controllers
{
    [Authorize( Roles = "Administrator")]
    public class LeaveTypesController : Controller
    {
        private readonly ILeaveTypeRepository _repo;
        private readonly IUnitofWork _unitofWork;
        private readonly IMapper _mapper;

        public LeaveTypesController(ILeaveTypeRepository repo, IUnitofWork unitofWork, IMapper mapper)
        {
            _repo = repo;
            _unitofWork = unitofWork;
            _mapper = mapper;
        }
        // GET: LeaveTypesController
        public async Task<ActionResult> Index()
        {
            //var leaveTypes =await _repo.FindAll();
            var leaveTypes =await _unitofWork.LeaveTypes.FindAll();
            var models = _mapper.Map<List<LeaveType>, List<LeaveTypeVM>>(leaveTypes.ToList());
            return View(models);
        }

        // GET: LeaveTypesController/Details/5
        public async Task<ActionResult> Details(int id)
        {
            //var isExists = await _repo.isExists(id);
            var isExists = await _unitofWork.LeaveTypes.isExists(q => q.Id == id);
            if (!isExists)
            {
                return NotFound();
            }
            //var leaveType = await _repo.FindById(id);
            var leaveType = await _unitofWork.LeaveTypes.Find(q => q.Id == id);
            var model = _mapper.Map<LeaveTypeVM>(leaveType);
            return View(model);
        }

        // GET: LeaveTypesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: LeaveTypesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(LeaveTypeVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var leaveType = _mapper.Map<LeaveType>(model);
                leaveType.DateCreated = DateTime.Now;

                //var isSuccess =await _repo.Create(leaveType);
                await _unitofWork.LeaveTypes.Create(leaveType);
                await _unitofWork.Save();

                
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Something Went Wrong");
                return View(model);
            }
        }

        // GET: LeaveTypesController/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            //var isExists = await _repo.isExists(id);
            var isExists = await _unitofWork.LeaveTypes.isExists(q => q.Id == id);
            if (!isExists)
            {
                return NotFound();
            }
            //var leaveType = await _repo.FindById(id);
            var leaveType = await _unitofWork.LeaveTypes.Find(q => q.Id == id);
            var model = _mapper.Map<LeaveTypeVM>(leaveType);
            return View(model);
        }

        // POST: LeaveTypesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(LeaveTypeVM model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                var leaveType = _mapper.Map<LeaveType>(model);
                
                _unitofWork.LeaveTypes.Update(leaveType);
                await _unitofWork.Save();

               // if (!isSuccess)
               // {
                //    ModelState.AddModelError("", "Something Went Wrong");
                //    return View(model);
               // }
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "Something Went Wrong");
                return View();
            }
        }

        
        // POST: LeaveTypesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, LeaveTypeVM model)
        {
            try
            {
               // var leaveType = await _repo.FindById(id);
                var leaveType = await _unitofWork.LeaveTypes.Find(q => q.Id == id);
                if (leaveType == null)
                {
                    return NotFound();
                }

                _unitofWork.LeaveTypes.Delete(leaveType);
                await _unitofWork.Save();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(model);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _unitofWork.Dispose();
            base.Dispose(disposing);
        }
    }


}
