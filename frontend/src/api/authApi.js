import axiosClient from './axiosClient';

const authApi = {
  login: (credentials) => {
    // credentials bao gồm: { userName, passWord }
    return axiosClient.post('/Auth/login', credentials);
  },
};

export default authApi;