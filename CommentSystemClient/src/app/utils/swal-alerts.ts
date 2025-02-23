import Swal from 'sweetalert2';

export class SwalAlerts {
  static showSubmitting(message: string) {
    Swal.fire({      
      text: message,
      allowOutsideClick: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });
  }

  static showError(message: string) {
    Swal.fire({
      icon: 'error',
      text: message,
      confirmButtonColor: '#d33',
      customClass: {
        popup: 'custom-swal'
      }
    });
  }
  
  static showSuccess(message: string, duration: number = 1000) {
    Swal.fire({
      icon: 'success',      
      showConfirmButton: false,
      timer: duration,
      customClass: {
        popup: 'custom-swal'
      }
    });
  } 

  static showInfo(message: string) {
    Swal.fire({
      icon: 'info',
      text: message,
      confirmButtonColor: '#d33',
      customClass: {
        popup: 'custom-swal'
      }
    });
  }

  static showWarning(message: string) {
    Swal.fire({
      icon: 'warning',
      text: message,
      confirmButtonColor: '#f0ad4e',
      customClass: {
        popup: 'custom-swal'
      }
    });
  }
}
