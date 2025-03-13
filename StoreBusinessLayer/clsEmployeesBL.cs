using StoreDataAccessLayer;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace StoreBusinessLayer
{
    public class clsEmployeesBL
    {
        public enum enMode { AddNew = 0, Update = 1 }
        public enMode Mode { get; set; } = enMode.AddNew;

        public EmployeeDTO DTO { get; set; }

        private readonly clsEmployeesDAL _employeesDAL;

        public clsEmployeesBL(EmployeeDTO dto, enMode mode = enMode.AddNew)
        {
            DTO = dto;
            Mode = mode;
            _employeesDAL = new clsEmployeesDAL(clsDataAccessSettingsDAL.CreateDataSource());
        }
        public clsEmployeesBL(clsEmployeesDAL employeesDAL)
        {
            _employeesDAL = employeesDAL;
        }



        public async Task<(List<EmployeeDTO> EmployeesList, int TotalCount)> GetEmployeesPaginatedWithFilters(
     int pageNumber, int pageSize, int? employeeID, string? userName, string? email,
     string? phone, string? role, bool? isActive)
        {
            return await _employeesDAL.GetEmployeesPaginatedWithFilters(pageNumber, pageSize, employeeID, userName, email, phone, role, isActive);
        }
        public async Task<clsEmployeesBL> FindEmployeeByEmployeeID(int id)
        {
            EmployeeDTO dto = await _employeesDAL.GetEmployeeByEmployeeID(id);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }


        private async Task<bool> _AddNewEmployee()
        {
            int employeeID = await _employeesDAL.AddEmployee(this.DTO);
            if (employeeID > 0)
            {
                this.DTO.EmployeeID = employeeID;
                return true;
            }
            return false;
        }

        private async Task<bool> _UpdateEmployee()
        {
            return await _employeesDAL.UpdateEmployee(this.DTO);
        }

        public async Task<bool> Save()
        {
            switch (this.Mode)
            {
                case enMode.AddNew:
                    if (await _AddNewEmployee())
                    {
                        this.Mode = enMode.Update;
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case enMode.Update:
                    return await _UpdateEmployee();

                default:
                    return false;
            }
        }

        public async Task<bool> DeleteEmployeeByEmployeeID(int id)
        {
            return await _employeesDAL.DeleteEmployeeByEmployeeID(id);
        }

        public async Task<bool> IsEmployeeExistsByEmployeeID(int id)
        {
            return await _employeesDAL.IsEmployeeExistsByEmployeeID(id);
        }

        public async Task<bool> IsEmployeeExistsByUserName(string userName)
        {
            return await _employeesDAL.IsEmployeeExistsByUserName(userName);
        }

        public async Task<clsEmployeesBL> GetEmployeeByEmailAndPassword(string email, string password)
        {
            EmployeeDTO dto = await _employeesDAL.GetEmployeeByEmailAndPassword(email, password);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public async Task<clsEmployeesBL> GetEmployeeByPhoneAndPassword(string phone, string password)
        {
            EmployeeDTO dto = await _employeesDAL.GetEmployeeByPhoneAndPassword(phone, password);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }

        public async Task<clsEmployeesBL> GetEmployeeByUserName(string userName)
        {
            EmployeeDTO dto = await _employeesDAL.GetEmployeeByUserName(userName);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }
        public async Task<clsEmployeesBL> GetEmployeeByEmail(string email)
        {
            EmployeeDTO dto = await _employeesDAL.GetEmployeeByEmail(email);

            if (dto != null)
            {
                return new clsEmployeesBL(dto, enMode.Update);
            }
            else
            {
                return null;
            }
        }
        public async Task<clsEmployeesBL> GetEmployeeByPhone(string phone)
        {
            {
                EmployeeDTO dto = await _employeesDAL.GetEmployeeByPhone(phone);

                if (dto != null)
                {
                    return new clsEmployeesBL(dto, enMode.Update);
                }
                else
                {
                    return null;
                }
            }
        }


        public async Task<bool> IsEmployeeAdmin(int employeeID)
        {
            return await _employeesDAL.IsEmployeeAdmin(employeeID);
        }
    }
}